# CLAUDE.md

このファイルはClaude Code（AIコーディングアシスタント）向けのプロジェクトガイダンスです。

コマンド仕様・スケジュール仕様・設定値管理の詳細は [`requirements.md`](./Docs/requirements.md) を参照してください。
クラス設計・シーケンス図・エラーハンドリング仕様の詳細は [`design.md`](./Docs/design.md) を参照してください。
BOTキャラクター・口調の詳細は [`character.md`](./Docs/character.md) を参照してください。
ディレクトリ構成の詳細は [`directory-structure.md`](./Docs/directory-structure.md) を参照してください。

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

### メッセージ

- BOTの応答文字列はすべて `Data/message.json` から読み込む
- コード内に応答文言をハードコードしない
- プレースホルダー（`{executeAt}`・`{toVc}`・`{fromVc}`・`{id}` 等）を `string.Format` または補間で展開する

---

## 開発上の注意

- BOTに必要なDiscord権限: `MoveMembers`、`SendMessages`、`UseApplicationCommands`
- ロールIDは `appsettings.json` で管理し、コードにハードコードしない
- 再起動によるスケジュール揮発はユーザー責任であり、永続化対応は行わない
- ファイル配置は `directory-structure.md` に従う
