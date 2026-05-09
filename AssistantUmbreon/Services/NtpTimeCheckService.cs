using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Services;

/// <summary>
/// ローカル時刻が NTP サーバーと同期されているかチェックする機能をホスティングサービスとして提供します。
/// </summary>
public class NtpTimeCheckService : IHostedService
{
    private const bool DefaultEnabled = true;
    private const string DefaultNtpServer = "ntp.nict.jp";
    private const uint DefalutAllowableMilliseconds = 1000;

    /// <summary>アプリケーション設定</summary>
    private readonly IConfiguration _config;
    /// <summary>ログ出力オブジェクト</summary>
    private readonly ILogger<NtpTimeCheckService> _logger;
    /// <summary>時刻同期チェックをする事を示すフラグ</summary>
    private readonly bool _enabled;
    /// <summary>時刻を取得する NTP サーバーのアドレス</summary>
    private readonly string _ntpServer;
    /// <summary>同期していると許容する、ローカル時刻と NTP サーバー時刻のミリ秒単位の絶対時間差</summary>
    private readonly uint _allowableMilliseconds;

    /// <summary>
    /// <see cref="NtpTimeCheckService"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="config">アプリケーション設定。</param>
    /// <param name="logger">ログ出力オブジェクト。</param>
    public NtpTimeCheckService(IConfiguration config, ILogger<NtpTimeCheckService> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _enabled = _config.GetValue("TimeSynchronizationCheck:Enabled", DefaultEnabled);
        _ntpServer = _config.GetValue("TimeSynchronizationCheck:NtpServer", DefaultNtpServer);
        _allowableMilliseconds = _config.GetValue("TimeSynchronizationCheck:AllowableMilliseconds", DefalutAllowableMilliseconds);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // 時刻同期チェックをしない場合は警告メッセージを出力して終了
        if (!_enabled)
        {
            _logger.LogWarning(_config["system:time_synchronization_check:disabled"]!);
            return;
        }

        // 取得先の NTP サーバーが設定されていない場合はエラー終了
        if (string.IsNullOrEmpty(_ntpServer))
        {
            var msg = _config["system:time_synchronization_check:npt_is_null"]!;
            _logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(_config["system:time_synchronization_check:start_checking"]!
            .Replace("{ntpServer}", _ntpServer));

        DateTimeOffset ntpTime;
        try
        {
            ntpTime = await GetNtpTimeAsync(_ntpServer, cancellationToken);
        }
        catch (Exception ex)
        {
            var msg = _config["system:time_synchronization_check:time_cannot_get"]!;
            _logger.LogCritical(ex, msg);
            throw new InvalidOperationException(msg, ex);
        }

        var systemTime = DateTimeOffset.UtcNow;
        uint diffMilliseconds = Convert.ToUInt32(Math.Ceiling(Math.Abs((ntpTime - systemTime).TotalMilliseconds)));
        if (diffMilliseconds > _allowableMilliseconds)
        {
            var jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            var format = "yyyy-MM-dd HH:mm:ss.fff";
            var msg = _config["system:time_synchronization_check:is_not_synchronized"]!
                .Replace("{allowableMilliseconds}", _allowableMilliseconds.ToString())
                .Replace("{ntpTime}", TimeZoneInfo.ConvertTime(ntpTime, jst).ToString(format))
                .Replace("{systemTime}", TimeZoneInfo.ConvertTime(systemTime, jst).ToString(format));
            _logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(_config["system:time_synchronization_check:success"]!
            .Replace("{diffMs}", diffMilliseconds.ToString()));
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    private async Task<DateTimeOffset> GetNtpTimeAsync(string server, CancellationToken cancellationToken)
    {
        const int ntpPort = 123;
        const int ntpPacketSize = 48;
        // NTPエポック(1900-01-01)とUnixエポック(1970-01-01)の差（秒）
        const long ntpEpochOffsetSeconds = 2208988800L;

        var ntpData = new byte[ntpPacketSize];
        ntpData[0] = 0x1B; // LI=0, VN=3, Mode=3 (client)

        // IPv4優先でアドレスを解決する
        var addresses = await Dns.GetHostAddressesAsync(server, cancellationToken);
        if (addresses.Length == 0)
            throw new InvalidOperationException(_config["system:time_synchronization_check:dns_resolve_failed"]!
                .Replace("{server}", server));

        var address = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork)
            ?? addresses[0];
        var endPoint = new IPEndPoint(address, ntpPort);

        using var udpClient = new UdpClient(address.AddressFamily);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

        await udpClient.SendAsync(ntpData, endPoint, timeoutCts.Token);

        UdpReceiveResult result;
        try
        {
            result = await udpClient.ReceiveAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(_config["system:time_synchronization_check:timeout"]!
                .Replace("{server}", server));
        }

        var data = result.Buffer;
        if (data.Length < ntpPacketSize)
            throw new InvalidDataException(_config["system:time_synchronization_check:invalid_packet"]!);

        // Transmit Timestamp（バイト 40-47）から時刻を取得
        var intPart = ((ulong)data[40] << 24) | ((ulong)data[41] << 16) | ((ulong)data[42] << 8) | data[43];
        var fracPart = ((ulong)data[44] << 24) | ((ulong)data[45] << 16) | ((ulong)data[46] << 8) | data[47];

        var unixSeconds = (long)intPart - ntpEpochOffsetSeconds;
        var fractionalMs = (long)(fracPart * 1000UL / 0x100000000UL);

        return DateTimeOffset.FromUnixTimeMilliseconds(unixSeconds * 1000 + fractionalMs);
    }
}
