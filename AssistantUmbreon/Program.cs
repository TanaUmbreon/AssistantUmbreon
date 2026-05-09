using AssistantUmbreon.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

// ホスト構築前のエラーをコンソールに出力するために、最小構成のログ出力を構築する
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

#if DEBUG
// 開発ビルド時のみ .env ファイルがあれば読み込んで環境変数として使用できるようにする
if (File.Exists(".env")) { Env.Load(); }
#endif

try
{
    // ホスト型のアプリケーションとして構築する
    // Tips:
    // - IHost.RunAsync メソッドを呼び出すことで、 IHost.Services プロパティ（DI コンテナ）に登録した IHostedService オブジェクトを自動的に開始する
    // - アプリケーションが終了するまで IHostedService オブジェクトはホストされ、自動的に停止の試みが行われる仕組み
    // - Host.CreateDefaultBuilder メソッドで構成される既定の設定は以下のページを参照
    //   https://learn.microsoft.com/ja-jp/dotnet/api/microsoft.extensions.hosting.host.createdefaultbuilder?view=net-10.0-pp#remarks
    IHost host = Host.CreateDefaultBuilder(args)
        // appsettings.json の Serilog キーに記述した設定を読み込み、ログ出力を構築する
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
            // WebSocket ベースの Discord クライアントを登録する
            services.AddSingleton(new DiscordSocketClient(
                new DiscordSocketConfig()
                {
                    GatewayIntents = GatewayIntents.Guilds |
                                     GatewayIntents.GuildVoiceStates |
                                     GatewayIntents.GuildMessages
                }));

            // 属性ベース（SlashCommand 属性）で実装したスラッシュコマンドのサービスを登録する
            services.AddSingleton(provider =>
                new InteractionService(
                    provider.GetRequiredService<DiscordSocketClient>(),
                    new InteractionServiceConfig()));

            // Hack: DIコンテナ化する必要ある？
            services.AddSingleton<MoveService>();

            // IHostedService オブジェクトを登録する
            services.AddHostedService<NtpTimeSynchronizationCheckService>();
            services.AddHostedService<BotService>();
        })
        .Build();

    await host.RunAsync();
}
finally
{
    await Log.CloseAndFlushAsync();
}
