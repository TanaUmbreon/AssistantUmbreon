# CLAUDE.md

このファイルはClaude Code（AIコーディングアシスタント）向けのプロジェクトガイダンスです。

コマンド仕様・スケジュール仕様・設定値管理の詳細は [`requirements.md`](./Docs/requirements.md) を参照してください。
クラス設計・シーケンス図・エラーハンドリング仕様の詳細は [`design.md`](./Docs/design.md) を参照してください。

---

## プロジェクト概要

`requirements.md` の「概要」セクションを参照。

---

## 技術スタック

`requirements.md` の「技術スタック」セクションを参照。

---

## 実装方針

### 設定値管理

`requirements.md` の「設定値管理」セクションを参照。

- コード内にトークンをハードコードしない
- `appsettings.json` のスキーマは `design.md` の「設定値管理」セクションを参照

### コマンド

`requirements.md` の「コマンド仕様」セクションを参照。

- スラッシュコマンドはギルドコマンドとして登録する（グローバルコマンドは反映に時間がかかるため）

### スケジュール

`requirements.md` の「スケジュール仕様」セクションを参照。

- タイムゾーンは必ずJST（`Asia/Tokyo` または `UTC+9`）で処理する

---

## クラス設計の決定事項

`design.md` の「クラス設計」セクションを参照。実装時に特に注意すべき決定事項を以下に示す。

### ScheduledJob は class

- `CancellationTokenSource` を自身のプロパティとして保持する
- `record` は使用しない（CTSという可変オブジェクトを持つため）

```csharp
public class ScheduledJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CancellationTokenSource Cts { get; } = new();
    // ... その他プロパティは design.md を参照
}
```

### MoveService はDIで注入する

- `PeroperoCommandModule`（即時実行）と `SchedulerService`（スケジュール実行）の両方から利用する
- VC移動ロジックを重複実装しない

### スケジューラの実装方針

- `Task.Delay` + `CancellationToken` でキャンセルを制御する
- `SchedulerService` 内でスレッドセーフなCRUDのために `SemaphoreSlim` を使用する
- 実行失敗時は `MoveService` の戻り値（`MoveResult`）でエラーを受け取り、`SchedulerService` が通知チャンネルに投稿する

### 移動元VCの確定タイミング

- コマンド実行時点で `fromVc` を確定し `ScheduledJob` に記録する
- スケジュール実行時に再取得しない

### 許可ロールの判定

- `AllowedRoleIds`（複数）と実行者のロールIDを照合する
- どれか1つでも一致すれば実行を許可する

---

## 開発上の注意

- BOTに必要なDiscord権限: `MoveMembers`、`SendMessages`、`UseApplicationCommands`
- ロールIDは `appsettings.json` で管理し、コードにハードコードしない
- 再起動によるスケジュール揮発はユーザー責任であり、永続化対応は行わない
