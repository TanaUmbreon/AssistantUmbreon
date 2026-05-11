---
name: add-command
description: /peroperoサブコマンド追加ガイド。新しいスラッシュコマンドを実装するとき、コマンドハンドラーを追加・修正するとき、権限チェックやエラーハンドリングのパターンを実装するときに必ず参照する。PeroperoCommandModule、InteractionModuleBase、MoveServiceの利用、AllowedRoleIdsの照合に関わる作業では必ずこのスキルを使う。
---

# コマンド追加

`/peropero` に新しいサブコマンドを追加するときのガイド。

## 参照ドキュメント

実装前に確認する:
- `Docs/requirements.md` — コマンド仕様セクション
- `Docs/design.md` — PeroperoCommandModule セクション

## 実装チェックリスト

### 権限チェック
コマンドハンドラーの冒頭で実行者のロールを照合する。

```
実行者のロールID一覧 ∩ AllowedRoleIds ≠ 空 → 実行許可
空 → Ephemeral でエラーメッセージを返す
```

`AllowedRoleIds` は `IConfiguration` 経由で取得する（コードへのハードコード禁止）。

### MoveService の利用
VC移動が必要な場合は `MoveService` を DI で注入して使う。移動ロジックを重複実装しない。

### エラーレスポンス
- メッセージは `message.json` から読む（文言のハードコード禁止）
- 権限エラー → Ephemeral
- その他エラー → コマンドを実行したテキストチャンネルに返す
