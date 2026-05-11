using AssistantUmbreon.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Services.Hosting;

public class SchedulerService : IHostedService
{
    private readonly List<ScheduledJob> _jobs = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly MoveService _moveService;
    private readonly DiscordSocketClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<SchedulerService> _logger;

    public SchedulerService(
        MoveService moveService,
        DiscordSocketClient client,
        IConfiguration config,
        ILogger<SchedulerService> logger)
    {
        _moveService = moveService;
        _client = client;
        _config = config;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _lock.WaitAsync(CancellationToken.None);
        try
        {
            foreach (var job in _jobs)
            {
                job.CancellationToken.Cancel();
            }
            _jobs.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> TryAddJobAsync(ScheduledJob job)
    {
        await _lock.WaitAsync();
        try
        {
            if (_jobs.Any(j => j.ExecuteAt == job.ExecuteAt))
                return false;
            _jobs.Add(job);
        }
        finally
        {
            _lock.Release();
        }

        _ = Task.Run(() => RunJobAsync(job));
        return true;
    }

    public IReadOnlyList<ScheduledJob> GetAllJobs()
    {
        _lock.Wait();
        try
        {
            return _jobs.ToList().AsReadOnly();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> CancelJobAsync(string id)
    {
        await _lock.WaitAsync();
        try
        {
            var job = _jobs.FirstOrDefault(j => j.Id.ToString("N")[..8] == id);
            if (job is null) return false;
            job.CancellationToken.Cancel();
            _jobs.Remove(job);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RunJobAsync(ScheduledJob job)
    {
        var shortId = job.Id.ToString("N")[..8];
        try
        {
            var delay = job.ExecuteAt - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, job.CancellationToken.Token);
            }

            var result = await _moveService.ExecuteAsync(job.FromVc, job.ToVc);

            if (result.IsSuccess)
            {
                if (job.ToVc is IMessageChannel toTextCh)
                {
                    var msg = _config["peropero:move:success_with_scheduled"]!
                        .Replace("{fromVc}", job.FromVc.Name)
                        .Replace("{id}", shortId);
                    await toTextCh.SendMessageAsync(msg);
                }
            }
            else
            {
                if (_client.GetChannel(job.NotifyChannelId) is IMessageChannel notifyCh)
                {
                    var msg = result.Error is null
                        ? _config["peropero:move:no_members"]!
                        : _config["peropero:move:failure_with_scheduled"]!.Replace("{id}", shortId);
                    await notifyCh.SendMessageAsync(msg);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "予約移動の実行中にエラーが発生しました（予約ID: {ShortId}）", shortId);
        }
        finally
        {
            await RemoveJobAsync(job.Id);
        }
    }

    private async Task RemoveJobAsync(Guid id)
    {
        await _lock.WaitAsync();
        try
        {
            var job = _jobs.FirstOrDefault(j => j.Id == id);
            if (job is not null) _jobs.Remove(job);
        }
        finally
        {
            _lock.Release();
        }
    }
}
