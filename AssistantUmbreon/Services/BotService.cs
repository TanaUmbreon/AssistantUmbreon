using AssistantUmbreon.Commands;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AssistantUmbreon.Services;

/// <summary>
/// ブラッキーくん BOT の機能をホストされたサービスとして提供します。
/// </summary>
public class BotService : IHostedService
{
    /// <summary>Discord クライアント</summary>
    private readonly DiscordSocketClient _client;
    /// <summary>Discord コマンドをサーバーに登録して実行するためのインタラクションサービス</summary>
    private readonly InteractionService _interactions;
    /// <summary>DI コンテナ</summary>
    private readonly IServiceProvider _services;
    /// <summary>アプリケーション設定</summary>
    private readonly IConfiguration _config;
    /// <summary>ログ出力オブジェクト</summary>
    private readonly ILogger _logger;

    /// <summary>
    /// <see cref="BotService"/> の新しいインスタンスを生成します。
    /// </summary>
    /// <param name="client">Web Socket ベースの Discord クライアント。</param>
    /// <param name="interactions">Discord コマンドをサーバーに登録して実行するためのインタラクションサービス。</param>
    /// <param name="services">DI コンテナ。</param>
    /// <param name="config">アプリケーション設定。</param>
    /// <param name="logger">ログ出力オブジェクト。</param>
    public BotService(DiscordSocketClient client, InteractionService interactions, IServiceProvider services, IConfiguration config, ILogger<BotService> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interactions = interactions ?? throw new ArgumentNullException(nameof(interactions));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // コマンドモジュールをインタラクションサービスに登録する
        await _interactions.AddModuleAsync<PeroperoCommandModule>(_services);

        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;
        _interactions.InteractionExecuted += OnInteractionExecutedAsync;

        // 環境変数が取得できない場合はアプリを停止する
        var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");
        if (token is null)
        {
            var msg = _config["system:environment:cannot_get"]!
                .Replace("{name}", "DISCORD_TOKEN");
            throw new InvalidOperationException(msg);
        }

        // Discord BOT にログインして接続を開始する
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

    /// <summary>
    /// Discord サーバーからインタラクションの実行要求が来たときに呼び出されます。
    /// </summary>
    /// <param name="interaction"></param>
    /// <returns></returns>
    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var context = new SocketInteractionContext(_client, interaction);
        await _interactions.ExecuteCommandAsync(context, _services);

        var commandName = interaction is SocketSlashCommand slashCmd
            ? BuildCommandString(slashCmd.Data.Name, slashCmd.Data.Options)
            : interaction.Type.ToString();

        _logger.LogInformation(
            "コマンド実行: {command} | 実行チャンネル: {channel} | 実行者: {GlobalName} (ユーザー名: {Username})",
            commandName,
            context.Channel.Name,
            context.User.GlobalName,
            context.User.Username);
    }

    private static string BuildCommandString(string name, IReadOnlyCollection<SocketSlashCommandDataOption> options)
    {
        var sb = new System.Text.StringBuilder($"/{name}");
        AppendOptions(sb, options);
        return sb.ToString();
    }

    private static void AppendOptions(System.Text.StringBuilder sb, IReadOnlyCollection<SocketSlashCommandDataOption>? options)
    {
        if (options is null or { Count: 0 }) return;
        foreach (var opt in options)
        {
            sb.Append($" {opt.Name}");
            if (opt.Type is ApplicationCommandOptionType.SubCommand or ApplicationCommandOptionType.SubCommandGroup)
                AppendOptions(sb, opt.Options);
            else
                sb.Append($":{opt.Value}");
        }
    }

    /// <summary>
    /// インタラクションの実行が成功または失敗した時に呼び出されます。
    /// </summary>
    /// <param name="context"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private async Task OnInteractionExecutedAsync(ICommandInfo _, IInteractionContext context, IResult result)
    {
        if (result.IsSuccess) { return; }

        var msg = _config["peropero:failure"]!;
        if (context.Interaction.HasResponded)
        {
            await context.Interaction.FollowupAsync(msg);
        }
        else
        {
            await context.Interaction.RespondAsync(msg);
        }
    }
}
