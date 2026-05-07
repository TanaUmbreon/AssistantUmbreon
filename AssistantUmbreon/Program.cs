using AssistantUmbreon;
using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// ホスト構築前のエラーをコンソールに出力するために、最小構成のロガーを作成して使用する
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

#if DEBUG
// 開発ビルド時のみ .env ファイルがあれば読み込んで環境変数として使用できるようにする
if (File.Exists(".env"))
{
    Env.Load();
}
#endif

try
{
    IHost host = Host.CreateDefaultBuilder(args)
        // appsettings.json の Serilog キーからログ出力の設定を読み込む
        .UseSerilog((context, _, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration))
        // メッセージファイルをコンフィグとして読み込みできるようにする
        .ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "data", "message.json"),
                optional: false,
                reloadOnChange: false);
        })
        // DI コンテナに必要なサービスを登録する
        .ConfigureServices((_, services) =>
        {
            services.AddSingleton(new DiscordSocketClient(
                new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.Guilds |
                                     GatewayIntents.GuildVoiceStates |
                                     GatewayIntents.GuildMessages
                }));

            // スラッシュコマンドを属性ベース（SlashCommand 属性）で実装できるようにする
            services.AddSingleton(provider =>
                new InteractionService(
                    provider.GetRequiredService<DiscordSocketClient>(),
                    new InteractionServiceConfig()));

            services.AddHostedService<NtpTimeCheckService>();
            services.AddSingleton<MoveService>();
            services.AddHostedService<BotService>();
        })
        .Build();

    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
