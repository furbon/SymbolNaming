# SymbolNaming

`SymbolNaming` は、シンボル名（変数名・プロパティ名・フィールド名など）を対象に、
**トークン分割 / 命名スタイル判定 / スタイル変換 / 補助的な検査** を行う .NET ライブラリです。

この README は、**最小サンプルで使い方を掴む → 提供機能を把握する → 詳細を確認する** 流れで読める構成にしています。

## 読み方ガイド

- すぐ使いたい: [`クイックスタート（Engine）`](#クイックスタートengine) と [`最小サンプル`](#最小サンプル)
- 何ができるか俯瞰したい: [`提供機能一覧`](#提供機能一覧)
- 実運用向けに深掘りしたい: [`実践ガイド`](#実践ガイド) と [`オプションと拡張ポイント`](#オプションと拡張ポイント)

## クイックスタート（Engine）

`SymbolCaseEngine` は、実運用で利用しやすい統合 API です。

```csharp
using SymbolNaming;
using SymbolNaming.Analysis;
using SymbolNaming.Conversion;
using SymbolNaming.Engine;
using SymbolNaming.Tokenization;

var engine = new SymbolCaseEngine(
    SymbolTokenizerFactory.CreateDefault(),
    new DefaultCaseClassifier(),
    new DefaultCaseConverter());

var result = engine.Analyze("UserName");
// result.Style == CaseStyle.PascalCase
// result.Prefixed == false
```

---

## 最小サンプル

### 1. スタイル判定（Analyze）

```csharp
var analyzed = engine.Analyze("UserName");
// analyzed.Style == CaseStyle.PascalCase
```

`Analyze` を使うと、入力シンボルがどの命名スタイルかを判定できます。

### 2. スタイル変換（Convert）

PascalCase の `"UserName"` を、SCREAMING_SNAKE_CASE の `"USER_NAME"` に変換するコードは以下の通りです。

```csharp
var converted = engine.Convert("UserName", CaseStyle.ScreamingSnakeCase);
// converted.Output == "USER_NAME"
```

### 3. 解析用の正規化（NormalizeForAnalysis）

```csharp
using SymbolNaming.Dictionaries;

var normalized = engine.NormalizeForAnalysis(
    "__m_user_name__",
    new CaseAnalysisOptions
    {
        PrefixProvider = new PrefixSetProvider("m"),
    });

// normalized.NormalizedSymbol == "user_name"
// normalized.Prefixed == true
// normalized.Prefix == "m"
```

`NormalizeForAnalysis` を使うと、prefix や装飾を考慮した「解析しやすい形」をすぐ取得できます。

### 4. 詳細検査（Inspect）

```csharp
using SymbolNaming.Dictionaries;

var inspection = engine.Inspect(
    "m_UserName",
    new CaseAnalysisOptions
    {
        PrefixProvider = new PrefixSetProvider("m"),
    });

// inspection.CaseStyle == CaseStyle.PascalCase
// inspection.Prefixed == true
// inspection.SymbolNameWithoutPrefix == "UserName"
```

`Inspect` では、スタイルだけでなく prefix 判定や prefix 除去後シンボルまで一度に取得できます。

---

## 提供機能一覧

### 基本機能

- `Analyze` / `TryAnalyze`: 命名スタイル判定（`Unknown` を含む曖昧ケース対応）
- `Convert`: `PascalCase` / `camelCase` / `snake_case` / `SCREAMING_SNAKE_CASE` など相互変換
- `Inspect`: 判定結果に加えて、Prefix 判定・装飾情報・警告情報を取得
- `NormalizeForAnalysis`: prefix / 装飾を扱いやすい形に正規化

### 高頻度実行向け

- `ReadOnlySpan<char>` / `ReadOnlyMemory<char>` オーバーロードを提供
- `TokenizeMany` / `AnalyzeMany` / `InspectMany` / `TryAnalyzeMany` / `ConvertMany` によるバルク処理
- 共有インスタンスを読み取り中心で使いやすい設計（並列実行を想定）

### 拡張とカスタマイズ

- `IPrefixProvider` / `IProtectedWordProvider` による辞書差し替え
- `IInspectionRule` による警告ルール拡張
- `CompositeSuffixPatternMatcher` による可変サフィックス判定
- `RuleBasedSymbolTokenizer` の直接構成（`Freeze()` 後に利用）

本ライブラリは、処理を以下の責務ごとに分離して設計されています。

- `Tokenization`: 文字列をルールベースでトークン分割
- `Analysis`: ケーススタイル判定、プレフィックス判定、装飾情報・複合パターン判定
- `Conversion`: 判定済みトークンを別スタイルへ変換
- `Engine`: 上記をまとめて扱う高水準 API
- `Dictionaries`: Prefix / ProtectedWord 判定の差し替え

## 対象範囲

本ライブラリは、**C#プログラミングにおいてシンボルとして扱える名前**を主対象にしています。

- 先頭が数字のパターンは対象外
- ハイフン（`-`）を含むパターンは対象外
- そのため `Kebab-case` のような、C#シンボルで扱えないスタイルは実装対象に含めていません

一方で、設計自体を C# 専用に限定しているわけではありません。
多くのプログラミング言語では C# と同様の命名規則を利用できるため、ライブラリはそれらにも適用できます。
本ライブラリのテストケースは C# コードを基準にしていますが、これは検証基準としての選択であり、
設計の本筋を C# 専用化する意図はありません。

## 動作環境と適用先

本ライブラリ本体は `netstandard2.0` をターゲットにしてビルドしています。

- ライブラリ実装ターゲット: `.NET Standard 2.0`
- テストプロジェクトターゲット: `.NET 10`

そのため、`netstandard2.0` を参照可能な .NET プロジェクトで利用できます。代表例:

- `.NET Framework 4.6.1` 以降
- `.NET Core 2.0` 以降
- `.NET 5+`（`.NET 6 / 7 / 8 / 9 / 10` を含む）

利用形態は、クラスライブラリ・コンソールアプリ・Web アプリ・ツール・アナライザ実装などを想定しています。

## 対応する命名スタイル

`CaseStyle` では次のスタイルをサポートします。

- `PascalCase`
- `CamelCase`
- `UpperSnakeCase`（例: `User_Name`）
- `LowerSnakeCase`（例: `user_name`）
- `ScreamingSnakeCase`（例: `USER_NAME`）
- `Unknown`（判定不能）

---

## 実践ガイド

最小サンプルで触れた API を、実務寄りの設定・入力値で段階的に確認します。

### 1. スタイル判定（Analyze / TryAnalyze）

```csharp
var analyze = engine.Analyze("__built_in_process__");
// analyze.Style == CaseStyle.LowerSnakeCase
// analyze.Decoration.LeadingUnderscoreCount == 2
// analyze.Decoration.TrailingUnderscoreCount == 2

var ok = engine.TryAnalyze("m_UserName", out var unknown);
// 既定設定では曖昧ケースを Unknown として扱う
```

`TryAnalyze` は判定不能時に `false` を返すため、`Unknown` を扱う処理フローを構成しやすくなります。

### 2. 変換（Convert）

```csharp
var camel = engine.Convert("UserName", CaseStyle.CamelCase);
// camel.Output == "userName"

var converted = engine.Convert(
    "m_UserName",
    CaseStyle.ScreamingSnakeCase,
    new CaseConversionOptions
    {
        PrefixPolicy = PrefixPolicy.Remove,
        AcronymPolicy = AcronymPolicy.Preserve,
    });
// converted.Output == "USER_NAME"
```

`CaseConversionOptions` で、プレフィックスの維持/除去/付与や頭字語ポリシーを制御できます。

### 3. 解析用正規化（NormalizeForAnalysis）

`NormalizeForAnalysis` は、`Analyze` / `Inspect` と同じ判定系を使って、
解析用途で扱いやすい正規化結果を返します。

```csharp
var normalized = engine.NormalizeForAnalysis(
    "__m_user_name__",
    new CaseAnalysisOptions
    {
        PrefixProvider = new PrefixSetProvider("m"),
    });

// normalized.NormalizedSymbol == "user_name"
// normalized.Prefixed == true
// normalized.Prefix == "m"
// normalized.LeadingUnderscoreCount == 2
// normalized.TrailingUnderscoreCount == 2
```

`LeadingUnderscoreCount` / `TrailingUnderscoreCount` / `Prefixed` / `Prefix` を利用することで、
利用側は `string.StartsWith` のような生文字列判定に依存せずに分岐できます。

### 4. 詳細検査（Inspect）

`Inspect` は、分類結果に加えて実用的な補助情報を返します。

```csharp
using SymbolNaming.Dictionaries;

var options = new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m"),
};

var inspection = engine.Inspect("m_UserName", options);

// inspection.CaseStyle == CaseStyle.PascalCase
// inspection.Prefixed == true
// inspection.HasPrefix == true
// inspection.Prefix == "m"
// inspection.SymbolNameWithoutPrefix == "UserName"
```

主な取得項目:

- `Prefix` / `SymbolNameWithoutPrefix`
- `LeadingUnderscoreCount` / `TrailingUnderscoreCount`
- `Warnings`（注意すべき分割パターン）
- `CompositePattern`（複合サフィックス一致時）

`CompositeSuffixPatternMatcher` を利用する場合、ルール構築時に `PatternId` の重複・空値が検証されます。

`Inspect` の警告判定は `IInspectionRule` で拡張できます。
`SymbolCaseEngine` 既定構成では `SymbolInspectionWarningAnalyzer` が組み込まれており、
必要に応じてコンストラクタでカスタムルール配列を指定できます。

```csharp
using SymbolNaming.Engine;

var engine = new SymbolCaseEngine(
    SymbolTokenizerFactory.CreateDefault(),
    new DefaultCaseClassifier(),
    new DefaultCaseConverter(),
    new IInspectionRule[]
    {
        new MyInspectionRule(),
    });
```

ルールは登録順に実行されるため、同一入力に対して `Warnings` の順序は決定的です。

### 5. Span / Memory ベース API

高頻度実行で割り当てを抑えたい場合は、`ReadOnlySpan<char>` / `ReadOnlyMemory<char>` オーバーロードを利用します。
`Analyze` / `TryAnalyze` / `Inspect` は `ReadOnlySpan<char>` を直接受け取れます。

```csharp
ReadOnlySpan<char> symbol = "m_UserName".AsSpan();

var analyzed = engine.Analyze(symbol, new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m"),
});

var ok = engine.TryAnalyze(symbol, out var result, new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m"),
});

var inspection = engine.Inspect(symbol, new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m"),
});

// inspection.Prefix は ReadOnlySpan<char>
// inspection.SymbolNameWithoutPrefix も ReadOnlySpan<char>
```

`ConvertMany` などのバルク API には `ReadOnlyMemory<char>` 入力も利用できます。

```csharp
var memoryInputs = new[]
{
    "UserName".AsMemory(),
    "HTTPServer".AsMemory(),
};

var converted = engine.ConvertMany(memoryInputs, CaseStyle.CamelCase);
```

- CPU・メモリ効率を重視し、不要な割り当てを抑える設計
- 並列実行時は、インスタンスを読み取り中心で共有しやすい構成

なお、本ライブラリはアナライザ専用ライブラリではありません。
通常のアプリケーションコードやツール実装でも同じ API を利用できます。

### 6. バルク処理 API（TokenizeMany / AnalyzeMany / InspectMany / TryAnalyzeMany / ConvertMany）

高頻度に大量シンボルを処理する場合は、`*Many` API を使うことで呼び出し側の反復オーバーヘッドを減らせます。

```csharp
var symbols = new[] { "UserName", "m_UserName", "__built_in_process__" };

var analyzeMany = engine.AnalyzeMany(
    symbols,
    new CaseAnalysisOptions
    {
        PrefixProvider = new PrefixSetProvider("m"),
    },
    BulkFailurePolicy.CollectErrors);

foreach (var item in analyzeMany)
{
    if (!item.IsSuccess)
    {
        // item.Error に例外情報を保持
        continue;
    }

    // item.Index は入力順のインデックス
    // item.Value は CaseClassificationResult
}

var inspectMany = engine.InspectMany(
    symbols,
    new CaseAnalysisOptions
    {
        PrefixProvider = new PrefixSetProvider("m"),
    });

var convertMany = engine.ConvertMany(symbols, CaseStyle.CamelCase);

var tryAnalyzeMany = engine.TryAnalyzeMany(symbols);
var tokenizeMany = engine.TokenizeMany(symbols);
```

`ReadOnlyMemory<char>` 入力例は、直前の `5. Span / Memory ベース API` を参照してください。

失敗時ポリシー:

- `BulkFailurePolicy.FailFast`: 最初の失敗で例外送出して中断
- `BulkFailurePolicy.CollectErrors`: 失敗を `BulkItemResult.Error` に保持して継続

契約:

- 返却順序は入力順を維持
- 各要素のインデックスは `BulkItemResult.Index` で取得
- `TryAnalyzeMany` は `BulkTryAnalyzeResult.Success` で判定可否を返却
- 共有インスタンスの並列利用時も、単一呼び出し内の結果順は入力順で決定的

### 7. トークナイザーを直接構成する場合（拡張利用）

通常の利用では、`SymbolCaseEngine` だけで完結します。

ただし、`RuleBasedSymbolTokenizer` を直接構成して使う場合は、
`AddRule(...)` でルールを設定したあとに `Freeze()` を呼び出して設定を確定します。
確定前に `Tokenize(...)` を呼ぶと例外がスローされます。

```csharp
using SymbolNaming.Tokenization;
using SymbolNaming.Tokenization.SplitRules;

var tokenizer = new RuleBasedSymbolTokenizer();
tokenizer.AddRule(new VerbatimRule());
tokenizer.AddRule(new UnderscoreRule());
tokenizer.AddRule(new UpperCaseRule());
tokenizer.AddRule(new PostDigitRule());

// ルール設定を確定
tokenizer.Freeze();

var tokens = tokenizer.Tokenize("UserName");
```

---

## オプションと拡張ポイント

### Prefix / ProtectedWord 判定

- `IPrefixProvider`: プレフィックス語の管理
- `IProtectedWordProvider`: 辞書語（プレフィックス扱いしない語）の管理

実務での利用イメージ:

- `str` は「構造体関連の接頭辞」としてプレフィックス側に登録
- `iOS` は「命名上保護したい語」として ProtectedWord 側に登録

```csharp
var analysisOptions = new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m", "s", "k", "str"),
    ProtectedWordProvider = new ProtectedWordSetProvider("iOS", "iPadOS"),
};

var inspection = engine.Inspect("str_UserData", analysisOptions);
// inspection.Prefixed == true
```

### 単一トークン曖昧判定ポリシー

`Player` のように複数スタイルに解釈可能な入力は、`AmbiguousSingleTokenPolicy` で挙動を選べます。

- `ReturnUnknown`（既定）
- `PreferSnakeCase`
- `PreferPascalOrCamel`
- `UseCustomResolver`

```csharp
var options = new CaseAnalysisOptions
{
    AmbiguousSingleTokenPolicy = AmbiguousSingleTokenPolicy.UseCustomResolver,
    AmbiguousSingleTokenResolver = matches =>
        matches.UpperSnakeCase ? CaseStyle.UpperSnakeCase : (CaseStyle?)null,
};
```

### ゲーム開発向け複合サフィックス（例: `EnemyUserData_WALK_NORMAL`）

`CompositeSuffixPatternMatcher` と `RegexCompositeSuffixPatternRule` で、
`ベース名 + _ + 可変サフィックス` 形式を判定できます。

```csharp
using SymbolNaming.Analysis;

var compositeMatcher = new CompositeSuffixPatternMatcher(
    new RegexCompositeSuffixPatternRule(
        "UpperTagOrSegments",
        "^[A-Z]+(?:_[A-Z0-9]+)*$",
        "^[A-Z][A-Za-z0-9]*$"),
    new RegexCompositeSuffixPatternRule(
        "PascalOrAlphaNumSegments",
        "^[A-Z][A-Za-z0-9]*(?:_[A-Za-z0-9]+)*$",
        "^[A-Z][A-Za-z0-9]*$"));

var inspect = engine.Inspect("EnemyUserData_WALK_NORMAL", new CaseAnalysisOptions
{
    CompositePatternMatcher = compositeMatcher,
});

// inspect.HasCompositePattern == true
// inspect.CompositePattern?.PatternId == "UpperTagOrSegments"
// inspect.CompositePatternBaseName == "EnemyUserData"
// inspect.CompositePatternSuffix == "WALK_NORMAL"
```

---

## まとめ

`SymbolNaming` は、命名規則の自動判定や統一変換を、
Prefix / ProtectedWord / 複合サフィックスなどの現場要件に合わせて拡張しやすい設計で提供しています。

とくに `SymbolCaseEngine` + `CaseAnalysisOptions` を起点に使うことで、
実プロジェクトの命名検査・変換処理を段階的に強化できます。
