using GuerrillaNtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Services;

/// <summary>
/// ローカル時刻が NTP サーバーと同期されているかチェックする機能をホスティングサービスとして提供します。
/// </summary>
public class NtpTimeSynchronizationCheckService : IHostedService
{
    private const bool DefaultEnabled = true;
    private const string DefaultNtpServer = "ntp.nict.jp";
    private const uint DefalutAllowableMilliseconds = 1000;

    /// <summary>アプリケーション設定</summary>
    private readonly IConfiguration _config;
    /// <summary>ログ出力オブジェクト</summary>
    private readonly ILogger<NtpTimeSynchronizationCheckService> _logger;
    /// <summary>時刻同期チェックをする事を示すフラグ</summary>
    private readonly bool _enabled;
    /// <summary>時刻を取得する NTP サーバーのアドレス</summary>
    private readonly string _ntpServer;
    /// <summary>同期していると許容する、ローカル時刻と NTP サーバー時刻のミリ秒単位の絶対時間差</summary>
    private readonly uint _allowableMilliseconds;

    /// <summary>
    /// <see cref="NtpTimeSynchronizationCheckService"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="config">アプリケーション設定。</param>
    /// <param name="logger">ログ出力オブジェクト。</param>
    public NtpTimeSynchronizationCheckService(IConfiguration config, ILogger<NtpTimeSynchronizationCheckService> logger)
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
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(_config["system:time_synchronization_check:start_checking"]!
            .Replace("{ntpServer}", _ntpServer));

        // NTP サーバーから時刻を取得する
        DateTimeOffset ntpTime;
        try
        {
            var client = new NtpClient(_ntpServer);
            var clock = await client.QueryAsync();
            ntpTime = clock.UtcNow;
        }
        catch (Exception ex)
        {
            var msg = _config["system:time_synchronization_check:time_cannot_get"]!;
            throw new InvalidOperationException(msg, ex);
        }

        var localTime = DateTimeOffset.UtcNow;
        uint diffMilliseconds = Convert.ToUInt32(Math.Ceiling(Math.Abs((ntpTime - localTime).TotalMilliseconds)));
        if (diffMilliseconds > _allowableMilliseconds)
        {
            var jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            var format = "yyyy-MM-dd HH:mm:ss.fff";
            var msg = _config["system:time_synchronization_check:is_not_synchronized"]!
                .Replace("{allowableMilliseconds}", _allowableMilliseconds.ToString())
                .Replace("{ntpTime}", TimeZoneInfo.ConvertTime(ntpTime, jst).ToString(format))
                .Replace("{systemTime}", TimeZoneInfo.ConvertTime(localTime, jst).ToString(format));
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(_config["system:time_synchronization_check:success"]!
            .Replace("{diffMs}", diffMilliseconds.ToString()));
    }

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
