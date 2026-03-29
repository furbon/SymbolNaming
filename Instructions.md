# SymbolNaming 開発仕様書

## 目的
`SymbolNaming` は C# シンボル名を対象に、以下を責務分離して扱うライブラリです。

- `Tokenization`: 文字列を `Token` 列へ分割
- `Analysis`: Case スタイル判定（+ Prefix 有無判定）
- `Conversion`: Case 変換
- `Dictionaries`: Prefix / Protected Word の辞書提供
- `Engine`: 上記を統合した高水準 API

本ドキュメントは、実装・テスト・過去の方針を統合した**開発仕様**です。

> Copilot 向けの応答・実装時ルールは `.github/copilot-instructions.md` を参照してください（本書から分離）。

## 対象構成（現行）
- ライブラリ: `SymbolNaming/SymbolNaming.csproj`（`.NET Standard 2.0` / `netstandard2.0`）
- テスト: `SymbolNaming.Test/SymbolNaming.Test.csproj`（`.NET 10` / `net10.0` + xUnit）
- 主要名前空間:
  - `SymbolNaming.Tokenization`
  - `SymbolNaming.Analysis`
  - `SymbolNaming.Conversion`
  - `SymbolNaming.Dictionaries`
  - `SymbolNaming.Engine`
  - `SymbolNaming.Tokens`

## 設計原則
1. **責務分離を維持する**
   - 分割・判定・変換を単一クラスへ集約しない。
   - `CaseRule` 的な集中設計は避け、拡張点を独立インターフェイスとして保持する。

2. **公開 API の中心は Engine**
   - `SymbolCaseEngine` を利用起点とし、`Tokenize` / `Analyze` / `Inspect` / `Convert` を提供する。

3. **Token は軽量に保つ**
   - `Token` は高頻度利用を前提に小さくシンプルに維持する。
   - 不要な内部文字列キャッシュ（例: `_inlineText`）は持たせない。

4. **未確定仕様はスタブ許容、確定仕様は維持**
   - 今後拡張予定の領域はスタブで可。
   - 既存の確定動作（特にトークナイズ挙動）を壊さない。

## 命名・プロジェクト方針
- ライブラリ名・プロジェクト名・アセンブリ名は `SymbolNaming` 系で統一する。
- 過去名称 `CaseRules` は `SymbolNaming` に統一する。

## Tokenization 方針
- ルールベース実装は `RuleBasedSymbolTokenizer` を基準とする。
- 分割ルールは `ISplitRule` で拡張可能にする。
- 現行主要ルール:
  - `VerbatimRule`（先頭 `@`）
  - `UpperCaseRule`（大文字境界）
  - `PostDigitRule`（数字→非数字境界）
  - `UnderscoreRule`（`_`）

### UpperCaseRule / PostDigitRule の責務
- `UpperCaseRule` は **digit 判定を持たない**。
- 数字境界は `PostDigitRule` 側に寄せる。

### 不要ルール
- ハイフン区切りルールは現時点で不要（追加しない）。

## Dictionaries（Prefix / Protected Word）方針
- Prefix 判定は生配列ではなく `IPrefixProvider` で扱う。
- Protected Word 判定は `IProtectedWordProvider` で扱う。
- Prefix 扱いは `PrefixProvider` または `TokenCategory.Prefix` に限定する。
- Case 分類時、Protected Word Provider に登録された単語は Prefix 扱いしない。

## Analysis 方針
- `DefaultCaseClassifier` は `TokenList` から `CaseClassificationResult` を返す。
- 命名スタイル判定に加え、Prefix 判定・装飾情報・複合サフィックス判定を責務として扱う。
- 正規化専用 API（`NormalizeForAnalysis`）と整合するよう、装飾情報（先頭/末尾アンダースコア）を結果モデルで保持する。
- 判定対象スタイル:
  - `PascalCase`
  - `CamelCase`
  - `UpperSnakeCase`
  - `LowerSnakeCase`
  - `ScreamingSnakeCase`
- 判定不能時は `CaseStyle.Unknown`。
- Prefix 判定結果は `Prefixed` に保持する。

## Conversion 方針
- `DefaultCaseConverter` は `TokenList` を入力に変換する。
- Prefix 制御は `PrefixPolicy`（`Keep` / `Remove` / `Add`）で行う。
- 略語制御は `AcronymPolicy`（`Preserve` / `Normalize`）で行う。
- ユーザー志向として、単純 `string` 戻り値のみではなく、将来は Result 型や Token ベース戻り値へ拡張しやすい設計を優先する。

## Engine 方針
- `SymbolCaseEngine` で Tokenize / Analyze / Convert を一貫提供する。
- `NormalizeForAnalysis` / `NormalizeForAnalysis(ReadOnlySpan<char>)` により、解析向け正規化シンボルと装飾情報を取得可能にする。
- `Inspect` / `Inspect(ReadOnlySpan<char>)` により、
  - 元シンボル
  - 分割トークン
  - 判定スタイル
  - Prefix 情報
  - Prefix 除去後シンボル
  を取得可能にする。
- `Inspect` の警告判定は `IInspectionRule` で拡張可能とし、既定ルールは `SymbolInspectionWarningAnalyzer` を利用する。
- ルール実行は登録順のパイプラインで行い、同一入力に対する `Warnings` の順序決定性を維持する。

## テスト運用方針
- xUnit テストを仕様の一次ソースとして扱う。
- 変更時は以下を重点確認する。
  - 既存トークナイズ境界（大文字・数字・`_`・`@`）
  - Prefix / Protected Word 競合時の優先順位
  - `Unknown` 判定と例外契約
  - Conversion の Prefix/Acronym ポリシー

## 運用メモ
- 会話由来の方針は、逐語転記ではなく仕様・設計意図として正規化して記述する。
- 未確定事項は「未確定」と明示し、確定仕様と混在させない。

## ドキュメント更新運用ルール
- 機能追加 PR では、実装変更と同一 PR 内で `README.md` と `Instructions.md` の両方を更新する。
- 仕様変更を伴う場合は、API 差分・利用者影響（移行要否）を `README.md` または仕様書に明記する。
