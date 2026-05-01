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

#if DEBUG
// 開発ビルド時のみ .env ファイルがあれば読み込んで Environment クラスから環境変数として使用できるようにする
if (File.Exists(".env"))
{
    Env.Load();
}
#endif

// ホスト構築前のエラーをコンソールに出力するための最小限のブートストラップロガー
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    IHost host = Host.CreateDefaultBuilder(args)
        .UseSerilog((context, _, loggerConfig) =>
            loggerConfig.ReadFrom.Configuration(context.Configuration))
        .ConfigureAppConfiguration((_, config) =>
        {
            // メッセージファイルをコンフィグ形式で読み込みできるようにする
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

            services.AddSingleton(provider =>
                new InteractionService(
                    provider.GetRequiredService<DiscordSocketClient>(),
                    new InteractionServiceConfig()
                    {
                        //
                    }));

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
