---
name: implement-scheduler
description: スケジューラ機能の実装・変更ガイド。SchedulerService、ScheduledJob、CancellationTokenSource、Task.Delay、SemaphoreSlimに関わる実装をするとき、スケジュール予約機能を追加・修正するとき、fromVcの確定タイミングや設計上の制約を確認したいときに必ず参照する。ScheduledJobをrecordにしようとしていたり、fromVcをスケジュール実行時に取得しようとしていたら、まずこのスキルを確認する。
---

# スケジューラ機能実装

`SchedulerService` または `ScheduledJob` を実装・変更するときのガイド。

## 参照ドキュメント

- `Docs/design.md` — SchedulerService・ScheduledJob セクション

## 設計上の決定事項（変更禁止）

### ScheduledJob は class（record 不可）
`CancellationTokenSource` は可変オブジェクトのため `record` は使えない。

```csharp
public class ScheduledJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public CancellationTokenSource CancellationToken { get; } = new();
    // その他プロパティは design.md を参照
}
```

### fromVc の確定タイミング
移動元VC（`fromVc`）はコマンド実行時点で確定し `ScheduledJob` に記録する。スケジュール実行時に再取得しない。

### MoveService は DI で注入する
`PeroperoCommandModule`（即時実行）と `SchedulerService`（スケジュール実行）の両方から利用する。移動ロジックを重複実装しない。

### スレッドセーフ CRUD
`SchedulerService` 内のリスト操作には `SemaphoreSlim(1, 1)` を使う。

### キャンセル制御
`Task.Delay(差分, job.CancellationToken.Token)` で待機し、`OperationCanceledException` をキャッチしてキャンセルを処理する。実行失敗時は `MoveResult` でエラーを受け取り、`SchedulerService` が通知チャンネルに投稿する。
