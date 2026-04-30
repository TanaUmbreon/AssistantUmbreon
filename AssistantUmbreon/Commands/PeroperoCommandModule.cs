using System.Diagnostics;
using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace AssistantUmbreon.Commands;

[Group("peropero", "ボイスチャンネル一斉移動 BOT のコマンド")]
public class PeroperoCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _config;
    private readonly MoveService _moveService;

    public PeroperoCommandModule(IConfiguration config, MoveService moveService)
    {
        _config = config;
        _moveService = moveService;
    }

    [SlashCommand("umbreon", "唐突にブラッキーくんをぺろぺろする。")]
    public async Task PeroperoAsync()
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!, ephemeral: true);
            return;
        }

        await RespondAsync(_config["peropero:default:success"]!);
    }

    [SlashCommand("move", "VC のメンバーを一斉移動する。")]
    public async Task MoveAsync(
        [Summary("to", "移動先のボイスチャンネル")] IVoiceChannel to)
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!, ephemeral: true);
            return;
        }

        if (Context.Channel is not IVoiceChannel fromVc)
        {
            await RespondAsync(_config["peropero:move:not_in_vc"]!);
            return;
        }

        await DeferAsync();

        var result = await _moveService.ExecuteAsync(fromVc, to);

        if (result.IsSuccess)
        {
            var msg = _config["peropero:move:success_with_immediate"]!
                .Replace("{fromVc}", fromVc.Name);

            if (to is IMessageChannel toTextCh)
                await toTextCh.SendMessageAsync(msg);

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

    private bool HasPermission()
    {
        var allowedRoleIds = _config.GetSection("Discord:AllowedRoleIds").Get<ulong[]>() ?? [];
        if (allowedRoleIds.Length == 0)
            return false;

        if (Context.User is not SocketGuildUser guildUser)
            return false;

        return guildUser.Roles.Any(r => allowedRoleIds.Contains(r.Id));
    }
}
