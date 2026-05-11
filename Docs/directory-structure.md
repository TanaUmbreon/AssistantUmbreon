# ディレクトリ構造

## リポジトリルート

- `.vscode/` — VSCode ワークスペース設定
  - `extensions.json` — 推奨拡張機能リスト
  - `settings.json` — ワークスペース設定
- `AssistantUmbreon/` — C# プロジェクト（後述）
- `Docs/` — ドキュメント（後述）
- `.gitignore` — Git 除外設定（.NET 標準）
- `AssistantUmbreon.slnx` — Visual Studio ソリューションファイル
- `CLAUDE.md` — Claude Code 向けプロジェクトガイダンス
- `dprint.json` — dprint コードフォーマッター設定（JSON 整形）

## C# プロジェクト (`AssistantUmbreon/`)

実装が進むにつれてサブディレクトリが追加される。`design.md` のアーキテクチャ概要も参照。

- `AssistantUmbreon.csproj` — プロジェクト定義（ターゲット: net10.0）
- `Program.cs` — エントリーポイント（HostBuilder + DI 設定）
- `Commands/` — スラッシュコマンド実装
  - `PeroperoCommandModule.cs` — `/peropero` 各サブコマンドのハンドラー
- `Services/` — ビジネスロジック
  - `MoveService.cs` — VC メンバー取得・移動実行
  - `Hosting/` — `IHostedService` 実装
    - `BotService.cs` — Discord 接続・スラッシュコマンド登録
    - `SchedulerService.cs` — スケジュール管理・実行
    - `NtpTimeSynchronizationCheckService.cs` — NTP 時刻同期チェック
- `Models/` — データモデル
  - `ScheduledJob.cs` — 予約データ＋`CancellationTokenSource`
- `Data/` — 静的データファイル
  - `message.json` — BOT 応答メッセージテンプレート（日本語）

### `Data/message.json`

コマンドごとの応答文字列をまとめたテンプレートファイル。プレースホルダー（`{executeAt}`・`{toVc}`・`{fromVc}`・`{id}` 等）を使用する。コード内に文言をハードコードしない。

## ドキュメント (`Docs/`)

- `requirements.md` — 要件定義（仕様・技術スタック・設定値管理）
- `design.md` — 設計書（アーキテクチャ・クラス設計・シーケンス図）
- `character.md` — BOT キャラクター設定
- `directory-structure.md` — 本ファイル
