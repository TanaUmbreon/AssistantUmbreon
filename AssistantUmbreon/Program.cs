using AssistantUmbreon;
using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

#if DEBUG
// 開発ビルド時のみ .env ファイルがあれば読み込んで Environment クラスから環境変数として使用できるようにする
if (File.Exists(".env"))
{
    Env.Load();
}
#endif

IHost host = Host.CreateDefaultBuilder(args)
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
