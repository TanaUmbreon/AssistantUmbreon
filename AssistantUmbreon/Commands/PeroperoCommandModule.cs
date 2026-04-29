using System.Diagnostics;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace AssistantUmbreon.Commands;

public class PeroperoCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _config;

    public PeroperoCommandModule(IConfiguration config)
    {
        _config = config;
    }

    [SlashCommand("peropero", "唐突にブラッキーくんをぺろぺろする。")]
    public async Task PeroperoAsync()
    {
        if (!HasPermission())
        {
            await RespondAsync(_config["peropero:no_permission_from_member"]!);
            return;
        }

        await RespondAsync(_config["peropero:default:success"]!);
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
