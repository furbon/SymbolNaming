# SymbolNaming ロードマップ（改訂版）

## 対象範囲と前提

- 本ロードマップは、現行の責務分離アーキテクチャに準拠する。
  - `Tokenization` / `Analysis` / `Conversion` / `Dictionaries` / `Engine`
- 非対象:
  - シンボル命名の分割・判定・変換の目的から外れる機能
  - C# 識別子対象外のハイフン区切り（例: `kebab-case`）
- ランタイム前提:
  - ライブラリ: `.NET Standard 2.0`
  - テスト: `.NET 10`

---

## 固定方針（確定）

1. **`Conversion` は破壊的変更を許容する**
   - `Convert` は Result 型へ一本化する。
   - `string` 戻り値 API との長期並存は行わない。

2. **初期段階から性能最適化を必須化する**
   - `Span` / `Memory` を全タスクの設計要件に含める（後回しにしない）。
   - アナライザのような高頻度・高並列実行を主用途として扱う。

3. **仕様・文書・実装の整合を最優先で解消する**
   - `*.md` / `*.csproj` / `README` の矛盾を先に解消する。

---

## マイルストーン

- **M0**: 仕様・文書の整合
- **M1**: `Conversion` Result 型一本化（破壊的変更）
- **M2**: `Inspect` 拡張点の公開
- **M3**: バルク処理 API（初版から `Span` / `Memory` 前提）
- **M4**: 正規化専用 API
- **M5**: `Composite Pattern` の事前検証・実行時最適化

---

## M0 — 仕様・文書整合

### ToDo

- [x] `Instructions.md` / `README.md` / `*.csproj` の TFM 記述を一致させる
- [x] 用語と責務説明を文書間で統一する
- [x] 機能追加 PR で README/仕様更新を必須化する運用ルールを追加する

### 受け入れ条件

- [x] TFM 記述に矛盾がない
- [x] 責務説明に矛盾がない
- [x] 更新運用ルールが明文化されている

---

## M1 — `Conversion` Result 型一本化（破壊的変更）

### ToDo

- [x] `CaseConversionResult` を追加する
  - [x] `Output`
  - [x] `AppliedPrefixPolicy`
  - [x] `AppliedAcronymPolicy`
  - [x] `Warnings`（または同等の診断情報）
- [x] `ICaseConverter` の戻り値を `string` から `CaseConversionResult` に変更する
- [x] `SymbolCaseEngine.Convert(...)` の戻り値も同様に変更する
- [x] 旧 `string` 戻り値 API は残さない
- [x] `README.md` に移行ガイドを追加する

### 性能要件（必須）

- [x] 変換処理の中間割り当てを最小化する
- [x] `ReadOnlySpan<char>` 入力経路を第一級 API として維持する
- [x] 単発変換スループットで回帰を出さない

### 受け入れ条件

- [x] 意図したコンパイル非互換を明示できる（Major 更新）
- [x] 既存の変換意味論（`PrefixPolicy` / `AcronymPolicy`）は維持される
- [x] 移行手順（`result.Output`）が README で明確化されている

---

## M2 — `Inspect` 拡張点（`IInspectionRule`）

### ToDo

- [x] 拡張ポイントとして `IInspectionRule` を追加する
- [x] 既存の `SymbolInspectionWarningAnalyzer` を既定ルールとして再構成する
- [x] `Engine` にルールパイプラインを追加する（責務集約はしない）

### 性能要件（必須）

- [x] ルール入力は `ReadOnlySpan<char>` + `TokenList` を主とする
- [x] ルール実行で不要割り当てを発生させない
- [x] 並列実行時に決定性を維持する

### 受け入れ条件

- [x] 既定警告の現行挙動が維持される
- [x] カスタムルール追加時も責務分離が崩れない
- [x] README に拡張方法と例が追記されている

---

## M3 — バルク処理 API（初版から `Span` / `Memory` 前提）

### ToDo

- [ ] `AnalyzeMany(...)` / `InspectMany(...)` / `TryAnalyzeMany(...)` / `ConvertMany(...)` を追加する
- [ ] `TokenizeMany(...)` は M3 で必要性評価し、必要なら同マイルストーン内で追加する
- [ ] 初版から `ReadOnlyMemory<char>` 系オーバーロードを提供する
- [ ] 順序保証・例外契約・失敗時ポリシー（fail-fast / collect-errors）を明文化する

### 性能要件（必須）

- [ ] 単発 API 反復呼び出しよりオーバーヘッドを削減する
- [ ] 高並列・高頻度実行を前提に設計する
- [ ] 大量入力時の割り当て増加を抑制する

### 受け入れ条件

- [ ] ベンチでスループット/割り当て改善を確認できる
- [ ] 並列利用時の契約が文書化されている
- [ ] README にアナライザ想定（`AnalyzeMany` / `InspectMany`）と変換想定（`ConvertMany`）の利用例がある

---

## M4 — 正規化専用 API

### ToDo

- [ ] `NormalizeForAnalysis`（仮名）を追加する
- [ ] 正規化結果と装飾情報（先頭/末尾アンダースコア等）を結果モデルとして返す
- [ ] 利用側が `string.StartsWith` 依存なしで判定できるようにする

### 性能要件（必須）

- [ ] 可能な限り `Span`/スライス情報中心で返す
- [ ] 余分な文字列生成を抑制する

### 受け入れ条件

- [ ] `Analyze` / `Inspect` と情報整合がある
- [ ] 特定ケース特化ではなく一般化設計になっている
- [ ] README で用途分担（正規化 vs 解析）が明確である

---

## M5 — `Composite Pattern` 事前検証・最適化

### ToDo

- [ ] ルール構築時の検証（不正パターン早期検出）を追加する
- [ ] 高頻度評価向けに実行時マッチング経路を最適化する
- [ ] 必要に応じてルール ID 重複などの整合チェックを追加する

### 性能要件（必須）

- [ ] 実行時の正規表現関連オーバーヘッドを削減する
- [ ] 反復呼び出し時の割り当てプロファイルを安定化する

### 受け入れ条件

- [ ] 既存の `CompositePatternMatcher` 契約互換を維持する
- [ ] ゲーム開発向け可変サフィックス要件を維持する
- [ ] 性能上の意図と結果が文書化されている

---

## 横断タスク（全マイルストーン共通）

- [ ] 責務分離を維持する
- [ ] 既存の確定挙動（特にトークナイズ）を壊さない
- [ ] 変更ごとにテストと README を同時更新する
- [ ] 共有インスタンスの並列利用前提を維持する
- [ ] 仕様未確定領域（例: `m__UserName`）は独断で確定しない

---

## 推奨実施順

1. `M0` 仕様・文書整合  
2. `M1` `Conversion` 破壊的変更  
3. `M2` `Inspect` 拡張  
4. `M5` `Composite Pattern` 最適化  
5. `M3` バルク API  
6. `M4` 正規化 API  

---

## バージョニング/リリース方針

- `M1` は **Major** 更新対象（破壊的変更）
- 以降は原則互換維持（破壊的変更時は明示）
- 各マイルストーン完了条件:
  - [ ] API 差分ノート
  - [ ] 移行ノート（必要時）
  - [ ] 性能ノート
  - [ ] README/仕様同期完了