# Copilot Instructions

## 役割分担
- `Instructions.md`: プロジェクトの仕様・設計・機能要件（人間向け）
- `.github/copilot-instructions.md`（このファイル）: Copilot の応答・実装時ルール（AI 向け）

## 応答ルール
- 回答は常に日本語で行う。
- 会話ログの逐語転記は行わず、意図を整理して記述する。
- 既存方針と矛盾する提案は避け、必要なら「未確定」と明示する。

## 実装ガイドライン
- 責務分離アーキテクチャ（`Tokenization` / `Analysis` / `Conversion` / `Dictionaries` / `Engine`）を維持する。
- `CaseRule` への機能集約は避け、分割・判定・変換を独立インターフェイスで拡張可能に保つ。
- `UpperCaseRule` では digit 判定を行わず、数字境界は `PostDigitRule` 側に寄せる。
- ハイフン区切りルールは不要。
- Token は高頻度利用を前提に軽量に保ち、不要メンバ（例: `_inlineText`）は追加しない。
- コア機能の安定性を重視しつつ、必要に応じた public インターフェイス変更は許容する。
- Conversion API は、単純な `string` 戻り値だけでなく将来拡張しやすい設計（Result 型や Token ベース）を優先する。
- Uppercase acronym 連続（例: `XML`）は現行 `UpperCaseRule` で不必要に細分化しない方針を維持し、必要時は辞書連携で補強する。
- Style判定では曖昧ケースをデフォルトで特定スタイルに寄せず、利用者が最終解決を選べるようにする（既定の特殊互換優先は避ける）。
- ゲーム開発向けに、基本スタイル（例: PascalCase）に対してアンダースコア+大文字サフィックス（例: EnemyUserData_WALK）を付ける特殊パターンを設計上サポートする。
- 設計方針として、責務分離を崩さずインターフェイスベースで拡張し、サフィックスは固定語だけでなく可変パターン（例: _WALK_NORMAL, _WALK_SP, _Attack_R2, _GuardAll）も対応可能にする。
- 命名解析の設計では特定ケース（例: _userName）だけに最適化せず、`__built_in_process` のような派生ケースも包含する一般化された設計を優先し、ケースごとの追加オプション増殖を避ける。
- 命名解析では正規化で判定しやすくしても、先頭記号（例: 先頭アンダースコア）の存在情報を結果モデルに保持し、利用側が `string.StartsWith` に頼らず判定できるAPIを求める。
- 仕様未定の命名パターン（特にPrefixとLeadingUnderscoreの相互作用）は独断でテスト化せず確認を取る。現時点でユーザーは `m__UserName` を `Unknown` 扱いが妥当と考えている。

## 命名と移行方針
- ライブラリ名・プロジェクト名・アセンブリ名は `SymbolNaming` 系に統一する。
- 過去名称 `CaseRules` から `SymbolNaming` への移行方針を維持する。

## Prefix Handling
- プレフィックス判定は、KnownPrefixesのような生配列ではなく、責務分離のためIPrefixProviderの専用インターフェイスで扱う。
- プレフィックス判定は PrefixProvider（または TokenCategory.Prefix）に限定する。Case分類では ProtectedWordProvider で辞書登録された単語はプレフィックス扱いしない。

## 変更時の優先順位
1. 既存の確定挙動（特にトークナイズ）を壊さない
2. 責務分離を崩さない
3. 将来拡張可能性を維持する
