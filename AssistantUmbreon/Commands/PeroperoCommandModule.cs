using AssistantUmbreon.Models;
using AssistantUmbreon.Services;
using AssistantUmbreon.Services.Hosting;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text;

namespace AssistantUmbreon.Commands;

/// <summary>
/// ブラッキーくん BOT を操作する為のスラッシュコマンドを提供します。
/// </summary>
[Group("peropero", "ブラッキーくん BOT のコマンドグループ")]
public class PeroperoCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly TimeZoneInfo Jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");

    /// <summary>アプリケーション設定</summary>
    private readonly IConfiguration _config;
    /// <summary>メンバー移動サービス</summary>
    private readonly MoveService _moveService;
    /// <summary>メンバー移動予約サービス</summary>
    private readonly SchedulerService _schedulerService;

    /// <summary>
    /// <see cref="PeroperoCommandModule"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="config">アプリケーション設定。</param>
    /// <param name="moveService">メンバー移動サービス。</param>
    /// <param name="schedulerService">メンバー移動予約サービス。</param>
    public PeroperoCommandModule(IConfiguration config, MoveService moveService, SchedulerService schedulerService)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _moveService = moveService ?? throw new ArgumentNullException(nameof(moveService));
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    /// <summary>
    /// ブラッキーくんを唐突にぺろぺろして困惑させます。
    /// </summary>
    [SlashCommand("umbreon", "唐突にブラッキーくんをぺろぺろするだけ。")]
    public async Task PeroperoAsync()
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        await RespondAsync(_config["peropero:default:success"]!);
    }

    [SlashCommand("move", "ボイスチャンネルに参加しているメンバーを一斉移動する。")]
    public async Task MoveAsync(
        [Summary("to", "移動先のボイスチャンネル")] IVoiceChannel toVc,
        [Summary("at", "実行日時（省略可。例: 19:07 または 2026-02-07 19:07）")] string? at = null)
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        if (Context.Channel is not IVoiceChannel fromVc)
        {
            await RespondAsync(_config["peropero:move:not_in_from_vc"]!);
            return;
        }

        if (toVc is null)
        {
            await RespondAsync(_config["peropero:move:not_specified_to_vc"]!);
            return;
        }

        if (fromVc.Id == toVc.Id)
        {
            await RespondAsync(_config["peropero:move:same_vc"]!);
            return;
        }

        // 予約実行
        if (at is not null)
        {
            await HandleScheduledMoveAsync(fromVc, toVc, at);
            return;
        }

        // 即時実行
        await DeferAsync();

        var result = await _moveService.ExecuteAsync(fromVc, toVc);

        if (result.IsSuccess)
        {
            var msg = _config["peropero:move:success_with_immediate"]!
                .Replace("{fromVc}", fromVc.Name);

            if (toVc is IMessageChannel toTextCh)
                await toTextCh.SendMessageAsync(msg);

            await DeleteOriginalResponseAsync();
        }
        else
        {
            var errorMsg = result.Error is null
                ? _config["peropero:move:no_members"]!
                : _config["peropero:move:failure_with_immediate"]!;

            await FollowupAsync(errorMsg);
        }
    }

    [SlashCommand("list", "ボイスチャンネル一斉移動の予約一覧を表示する。")]
    public async Task ListAsync()
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        var jobs = _schedulerService.GetAllJobs();

        if (jobs.Count == 0)
        {
            await RespondAsync(_config["peropero:list:no_jobs"]!);
            return;
        }

        var sb = new StringBuilder(_config["peropero:list:has_jobs"]!);
        sb.AppendLine();
        foreach (var job in jobs)
        {
            var shortId = job.Id.ToString("N")[..8];
            var executeAtJst = TimeZoneInfo.ConvertTime(job.ExecuteAt, Jst);
            sb.AppendLine($"・`{shortId}` | {job.FromVc.Name} → {job.ToVc.Name} | {executeAtJst:yyyy-MM-dd HH:mm}");
        }

        await RespondAsync(sb.ToString());
    }

    [SlashCommand("cancel", "ボイスチャンネル一斉移動の予約をキャンセルする。")]
    public async Task CancelAsync([Summary("id", "キャンセルする予約ID")] string id)
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        var cancelled = await _schedulerService.CancelJobAsync(id);

        var msg = cancelled
            ? _config["peropero:cancel:success"]!.Replace("{id}", id)
            : _config["peropero:cancel:not_found"]!.Replace("{id}", id);

        await RespondAsync(msg);
    }

    private async Task HandleScheduledMoveAsync(IVoiceChannel fromVc, IVoiceChannel toVc, string at)
    {
        var executeAt = ParseJstDateTime(at);
        if (executeAt is null)
        {
            await RespondAsync(_config["peropero:move:invalid_format"]!);
            return;
        }

        if (executeAt <= DateTimeOffset.UtcNow)
        {
            await RespondAsync(_config["peropero:move:past_datetime"]!);
            return;
        }

        var members = (await fromVc.GetUsersAsync().FlattenAsync())
            .Where(u => u.VoiceChannel?.Id == fromVc.Id)
            .ToList();

        if (members.Count == 0)
        {
            await RespondAsync(_config["peropero:move:no_members"]!);
            return;
        }

        var job = new ScheduledJob
        {
            FromVc = fromVc,
            ToVc = toVc,
            ExecuteAt = executeAt.Value,
            RequestedBy = Context.User.Id,
            NotifyChannelId = Context.Channel.Id,
        };

        if (!await _schedulerService.TryAddJobAsync(job))
        {
            await RespondAsync(_config["peropero:move:duplicate_time"]!);
            return;
        }

        var shortId = job.Id.ToString("N")[..8];
        var executeAtJst = TimeZoneInfo.ConvertTime(executeAt.Value, Jst);
        var msg = _config["peropero:move:registered"]!
            .Replace("{executeAt}", executeAtJst.ToString("yyyy-MM-dd HH:mm"))
            .Replace("{toVc}", toVc.Name)
            .Replace("{id}", shortId);

        await RespondAsync(msg);
    }

    private static DateTimeOffset? ParseJstDateTime(string at)
    {
        var trimmed = at.Trim();

        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var fullDt))
        {
            return new DateTimeOffset(fullDt, TimeSpan.FromHours(9));
        }

        if (DateTime.TryParseExact(trimmed, "HH:mm",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var timePart))
        {
            var nowJst = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(9));
            var dt = nowJst.Date.Add(timePart.TimeOfDay);
            return new DateTimeOffset(dt, TimeSpan.FromHours(9));
        }

        return null;
    }

    /// <summary>
    /// 現在のコンテキストのユーザーが、この BOT のコマンド実行権限を持っている事を判定します。
    /// </summary>
    private bool HasPermission()
    {
        if (Context.User is not SocketGuildUser guildUser) return false;

        var rawIds = Environment.GetEnvironmentVariable("ALLOWED_ROLE_IDS") ?? "";
        var allowedRoleIds = rawIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => ulong.TryParse(s, out var id) ? id : 0UL)
            .Where(id => id != 0)
            .ToArray();

        if (allowedRoleIds.Length == 0) return false;

        return guildUser.Roles.Any(r => allowedRoleIds.Contains(r.Id));
    }
}
