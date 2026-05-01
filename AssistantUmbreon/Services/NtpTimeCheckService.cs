using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Services;

public class NtpTimeCheckService : IHostedService
{
    private readonly IConfiguration _config;
    private readonly ILogger<NtpTimeCheckService> _logger;

    public NtpTimeCheckService(IConfiguration config, ILogger<NtpTimeCheckService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_config.GetValue<bool>("TimeSynchronizationCheck:Enabled"))
        {
            _logger.LogWarning(_config["system:time_synchronization_check:disabled"]!);
            return;
        }

        var ntpServer = _config["TimeSynchronizationCheck:NtpServer"];
        if (ntpServer is null)
        {
            var msg = _config["system:time_synchronization_check:npt_is_null"]!;
            _logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        var allowableMs = _config.GetValue<int>("TimeSynchronizationCheck:AllowableMilliseconds");

        _logger.LogInformation(_config["system:time_synchronization_check:start_checking"]!
            .Replace("{ntpServer}", ntpServer));

        DateTimeOffset ntpTime;
        try
        {
            ntpTime = await GetNtpTimeAsync(ntpServer, cancellationToken);
        }
        catch (Exception ex)
        {
            var msg = _config["system:time_synchronization_check:time_cannot_get"]!;
            _logger.LogCritical(ex, msg);
            throw new InvalidOperationException(msg, ex);
        }

        var systemTime = DateTimeOffset.UtcNow;
        var diffMs = Math.Abs((ntpTime - systemTime).TotalMilliseconds);

        if (diffMs > allowableMs)
        {
            var jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
            var fmt = "yyyy-MM-dd HH:mm:ss.fff";
            var msg = _config["system:time_synchronization_check:is_not_synchronized"]!
                .Replace("{allowableMilliseconds}", allowableMs.ToString())
                .Replace("{ntpTime}", TimeZoneInfo.ConvertTime(ntpTime, jst).ToString(fmt))
                .Replace("{systemTime}", TimeZoneInfo.ConvertTime(systemTime, jst).ToString(fmt));
            _logger.LogCritical(msg);
            throw new InvalidOperationException(msg);
        }

        _logger.LogInformation(_config["system:time_synchronization_check:success"]!
            .Replace("{diffMs}", $"{diffMs:F0}"));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

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
