using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Commands;

/// <summary>
/// ブラッキーくん BOT を操作する為のスラッシュコマンドを提供します。
/// </summary>
[Group("peropero", "ブラッキーくん BOT のコマンドグループ")]
public class PeroperoCommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _config;
    private readonly MoveService _moveService;
    private readonly ILogger<PeroperoCommandModule> _logger;

    /// <summary>
    /// <see cref="PeroperoCommandModule"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="config">コンフィグ。</param>
    /// <param name="moveService"></param>
    /// <param name="logger"></param>
    public PeroperoCommandModule(IConfiguration config, MoveService moveService, ILogger<PeroperoCommandModule> logger)
    {
        _config = config;
        _moveService = moveService;
        _logger = logger;
    }

    /// <summary>
    /// ブラッキーくんを唐突にぺろぺろして困惑させます。
    /// </summary>
    /// <returns>戻り値なしの非同期操作。</returns>
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
    public async Task MoveAsync([Summary("to", "移動先のボイスチャンネル")]IVoiceChannel toVc)
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

    /// <summary>
    /// 現在のコンテキストのユーザーが、この BOT のコマンド実行権限を持っている事を判定します。
    /// </summary>
    /// <returns>実行権限を持っている場合は true、持っていない場合は false。</returns>
    private bool HasPermission()
    {
        // ユーザーがサーバーのメンバーでない場合は実行権限なし
        if (Context.User is not SocketGuildUser guildUser) { return false; }

        // 環境変数からコマンド実行を許可するロール ID を取得する
        var rawIds = Environment.GetEnvironmentVariable("ALLOWED_ROLE_IDS") ?? "";
        var allowedRoleIds = rawIds
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => ulong.TryParse(s, out var id) ? id : 0UL)
            .Where(id => id != 0)
            .ToArray();

        if (allowedRoleIds.Length == 0) { return false; }

        return guildUser.Roles.Any(r => allowedRoleIds.Contains(r.Id));
    }
}
