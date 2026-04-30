# Assistant Umbreon

友人・仲間内の Discord サーバー向け、ボイスチャンネル一斉移動 BOT です。  
我が家のブラッキーくんが、指定したボイスチャンネルへメンバーを一斉に移動するお手伝いをしてくれます。

## 1. アプリ概要

### 1.1. Discord サーバーに追加される BOT コマンド

| コマンド | 説明 |
|---------|------|
| `/peropero umbreon` | ブラッキーくんにぺろぺろする |
| `/peropero move <to> [at]` | ボイスチャンネルのメンバーを一斉移動（即時移動または予約移動） |
| `/peropero list` | 予約移動のスケジュール表示 |
| `/peropero cancel <id>` | 予約移動のキャンセル |

> **⚠️注意**: 以下のコマンドは現バージョンで未実装です。

- `/peropero move`: 予約実行が未実装（即時実行は可能）
- `/peropero list`
- `/peropero cancel`

> **⚠️注意**: BOT の再起動で予約移動のスケジュールは失われます。再起動後は再予約が必要です。

## 2. アプリ実行に必要なもの

### 2.1. 利用者側で用意するもの

- Discord の設定:
  - [Discord Developer Portal](https://discord.com/developers/home) で作成した BOT のトークン。
  - 上記 BOT を招待済みの Discord サーバー。
  - コマンド実行を許可する任意のロール。
- 実行端末の設定:
  - Docker および Docker Compose（本番環境向け）。
    - または [.NET 10](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0) の「.NET Runtime」がインストールされた PC（ローカル実行環境向け）。
    - または [.NET 10](https://dotnet.microsoft.com/ja-jp/download/dotnet/10.0) の「SDK」がインストールされた PC（開発者向け）。

> **⚠️注意**: このアプリは実行している間のみ、 Discord サーバー内でコマンドが使えるようになります。そのためアプリは常時稼働している前提です。

### 2.2. BOT に必要なスコープと権限

Discord Developer Portal の「OAuth」ページにある「OAuth2 URLジェネレーター」で以下の設定を行い、「生成されたURL」から BOT を Discord サーバーに招待する。

| スコープ |
| :------- |
| `bot` |

| 権限 | 用途 |
| :--- | :--- |
| `Move Members`<br>(`メンバーを移動`) | VC メンバーの移動 |
| `Send Messages`<br>(`メッセージを送る`) | テキストチャンネルへの通知 |
| `Use Application Commands`<br>(`スラッシュコマンドを使用`) | スラッシュコマンドの受信 |

### 2.3. 環境変数の設定

実行する端末に以下の環境変数を設定してください。

| 変数名 | 説明 | 例 |
|--------|------|----|
| `DISCORD_TOKEN` | BOT のトークン | `MTEx...` |
| `GUILD_ID` | 接続先サーバーの ID | `123456789012345678` |
| `ALLOWED_ROLE_IDS` | コマンド実行を許可するロール ID（カンマ区切りで複数指定可） | `111...,222...` |

### 2.4. 通常設定の確認（任意）

アプリに含まれる `appsettings.json` ファイルから NTP サーバーや許容時刻誤差を設定できます。

```json
{
  "TimeSynchronization": {
    "NtpServer": "ntp.nict.jp",
    "AllowableMilliseconds": 1000
  }
}
```

## 3. 免責事項

- 本ソフトウェアは個人的な利用を目的として開発されており、いかなる保証も提供しません。
- 本ソフトウェアの使用によって生じたいかなる損害（データ損失、サービス中断、Discord アカウント・サーバーへの影響を含むがこれに限らない）についても、作者は一切の責任を負いません。
- Discord の利用規約・開発者ポリシーを遵守した上でご利用ください。
- スケジュール機能はインメモリで動作するため、BOT の再起動によって予約データは失われます。予約の管理はユーザーの責任において行ってください。
- 本ソフトウェアは現状有姿（AS IS）で提供されます。

## 4. リリースノート

### Version x.x.x (yyyy-mm-dd)

- xxxxx

## 5. ライセンス情報

本ソフトウェアが使用している OSS ライブラリのライセンス表記です。

### Discord.Net

- バージョン: 3.19.1
- ライセンス: MIT License
- 著作権: Copyright (c) 2015-2022 Discord.Net Contributors
- ソース: https://github.com/discord-net/Discord.Net

### DotNetEnv

- バージョン: 3.2.0
- ライセンス: MIT License
- 著作権: Copyright (c) Toni Solarin-Sodara
- ソース: https://github.com/tonerdo/dotnet-env

### Microsoft.Extensions.Hosting

- バージョン: 10.0.7
- ライセンス: MIT License
- 著作権: Copyright (c) .NET Foundation and Contributors
- ソース: https://github.com/dotnet/runtime

MIT License の全文は各リポジトリの `LICENSE` ファイルを参照してください。
