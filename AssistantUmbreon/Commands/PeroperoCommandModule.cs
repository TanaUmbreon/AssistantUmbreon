using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Commands;

[Group("peropero", "ボイスチャンネル一斉移動 BOT のコマンド")]
public class PeroperoCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _config;
    private readonly MoveService _moveService;
    private readonly ILogger<PeroperoCommandModule> _logger;

    public PeroperoCommandModule(IConfiguration config, MoveService moveService, ILogger<PeroperoCommandModule> logger)
    {
        _config = config;
        _moveService = moveService;
        _logger = logger;
    }

    [SlashCommand("umbreon", "唐突にブラッキーくんをぺろぺろするだけ。")]
    public async Task PeroperoAsync()
    {
        LogCommandExecution("peropero umbreon");

        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        await RespondAsync(_config["peropero:default:success"]!);
    }

    [SlashCommand("move", "ボイスチャンネルに参加しているメンバーを一斉移動する。")]
    public async Task MoveAsync([Summary("to", "移動先のボイスチャンネル")]IVoiceChannel toVc)
    {
        LogCommandExecution("peropero move");

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

        await DeferAsync();

        var result = await _moveService.ExecuteAsync(fromVc, toVc);

        if (result.IsSuccess)
        {
            var msg = _config["peropero:move:success_with_immediate"]!
                .Replace("{fromVc}", fromVc.Name);

            if (toVc is IMessageChannel toTextCh)
            {
                await toTextCh.SendMessageAsync(msg);
            }

            await DeleteOriginalResponseAsync();
        }
        else
        {
            var errorMsg = result.ErrorMessage == "no_members"
                ? _config["peropero:move:no_members"]!
                : _config["peropero:move:failure_with_immediate"]!;

            await FollowupAsync(errorMsg);
        }
    }

    private void LogCommandExecution(string commandName)
    {
        var jst = TimeZoneInfo.FindSystemTimeZoneById("Asia/Tokyo");
        var executedAt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, jst);
        _logger.LogInformation(
            "コマンド実行: /{Command} | ユーザー: {GlobalName} (Username: {Username} | ID: {UserId}) | 実行日時: {ExecutedAt:yyyy-MM-dd HH:mm:ss} JST",
            commandName,
            Context.User.GlobalName,
            Context.User.Username,
            Context.User.Id,
            executedAt);
    }

    private bool HasPermission()
    {
        var raw = Environment.GetEnvironmentVariable("ALLOWED_ROLE_IDS") ?? "";
        var allowedRoleIds = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => ulong.TryParse(s, out var id) ? id : 0UL)
            .Where(id => id != 0)
            .ToArray();

        if (allowedRoleIds.Length == 0)
            return false;

        if (Context.User is not SocketGuildUser guildUser)
            return false;

        return guildUser.Roles.Any(r => allowedRoleIds.Contains(r.Id));
    }
}
