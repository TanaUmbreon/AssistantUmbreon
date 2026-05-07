using AssistantUmbreon.Commands;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AssistantUmbreon.Services;

/// <summary>
/// Discord BOT の開始から終了までのライフサイクルを実装します。
/// </summary>
public class BotService : IHostedService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;

    /// <summary>
    /// <see cref="BotService"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="client">Web Socket ベースの Discord クライアント。</param>
    /// <param name="interactions"></param>
    /// <param name="services"></param>
    /// <param name="config"></param>
    public BotService(DiscordSocketClient client, InteractionService interactions, IServiceProvider services, IConfiguration config)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _config = config;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _interactions.AddModuleAsync<PeroperoCommandModule>(_services);

        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _interactions.InteractionExecuted += OnInteractionExecutedAsync;

        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")
            ?? throw new InvalidOperationException("環境変数 'DISCORD_TOKEN' が設定されていません。");

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
    }

    /// <summary>
    /// Discord クライアントが接続完了になった時に呼び出されます。
    /// </summary>
    /// <returns></returns>
    private async Task OnReadyAsync()
    {
        // スラッシュコマンドの登録対象サーバーを取得する
        var guildIdStr = Environment.GetEnvironmentVariable("GUILD_ID")
            ?? throw new InvalidOperationException("環境変数 'GUILD_ID' が設定されていません。");
        var guildId = ulong.Parse(guildIdStr);

        // サーバーにスラッシュコマンドを登録する
        await _interactions.RegisterCommandsToGuildAsync(guildId);
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _services);
    }

    private async Task OnInteractionExecutedAsync(ICommandInfo _, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) { return; }

        var message = _config["peropero:failure"]!;

        if (context.Interaction.HasResponded)
        {
            await context.Interaction.FollowupAsync(message);
        }
        else
        {
            await context.Interaction.RespondAsync(message);
        }
    }
}
