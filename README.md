# SymbolNaming

`SymbolNaming` は、シンボル名（変数名・プロパティ名・フィールド名など）を対象に、
**トークン分割 / 命名スタイル判定 / スタイル変換 / 補助的な検査** を行う .NET ライブラリです。

本ライブラリは、処理を以下の責務に分離して設計されています。

- `Tokenization`: 文字列をルールベースでトークン分割
- `Analysis`: ケーススタイル判定、プレフィックス判定、装飾情報・複合パターン判定
- `Conversion`: 判定済みトークンを別スタイルへ変換
- `Engine`: 上記をまとめて扱う高水準 API
- `Dictionaries`: Prefix / ProtectedWord 判定の差し替え

---

## 対応する命名スタイル

`CaseStyle` として次を扱います。

- `PascalCase`
- `CamelCase`
- `UpperSnakeCase`（例: `User_Name`）
- `LowerSnakeCase`（例: `user_name`）
- `ScreamingSnakeCase`（例: `USER_NAME`）
- `Unknown`（判定不能）

---

## クイックスタート（Engine）

`SymbolCaseEngine` は実運用で最も使いやすい統合 API です。

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

## 具体的な使い方

### 1. スタイル判定（Analyze / TryAnalyze）

```csharp
var analyze = engine.Analyze("__built_in_process__");
// analyze.Style == CaseStyle.LowerSnakeCase
// analyze.Decoration.LeadingUnderscoreCount == 2
// analyze.Decoration.TrailingUnderscoreCount == 2

var ok = engine.TryAnalyze("m_UserName", out var unknown);
// 既定設定では曖昧ケースを Unknown として扱う
```

`TryAnalyze` は判定不能時に `false` を返し、`Unknown` を扱うフローを組みやすくできます。

### 2. 変換（Convert）

```csharp
var camel = engine.Convert("UserName", CaseStyle.CamelCase);
// "userName"

var converted = engine.Convert(
    "m_UserName",
    CaseStyle.ScreamingSnakeCase,
    new CaseConversionOptions
    {
        PrefixPolicy = PrefixPolicy.Remove,
        AcronymPolicy = AcronymPolicy.Preserve,
    });
// "USER_NAME"
```

`CaseConversionOptions` で、プレフィックスの維持/除去/付与や頭字語ポリシーを制御できます。

### 3. 詳細検査（Inspect）

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

取得できる代表情報:

- `Prefix` / `SymbolNameWithoutPrefix`
- `LeadingUnderscoreCount` / `TrailingUnderscoreCount`
- `Warnings`（注意すべき分割パターン）
- `CompositePattern`（複合サフィックス一致時）

### 4. Span ベース API

割り当てを抑えたい場面では `ReadOnlySpan<char>` ベース API を使えます。

```csharp
ReadOnlySpan<char> input = "s_UserName".AsSpan();
var inspection = engine.Inspect(input, new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("s_"),
});

// inspection.Prefix は ReadOnlySpan<char>
// inspection.SymbolNameWithoutPrefix も ReadOnlySpan<char>
```

---

## オプションと拡張ポイント

### Prefix / ProtectedWord 判定

- `IPrefixProvider`: プレフィックス語の管理
- `IProtectedWordProvider`: 辞書語（プレフィックス扱いしない語）の管理

```csharp
var analysisOptions = new CaseAnalysisOptions
{
    PrefixProvider = new PrefixSetProvider("m", "s", "k"),
    ProtectedWordProvider = new ProtectedWordSetProvider("str", "ptr"),
};
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
Prefix / ProtectedWord / 複合サフィックスなどの現場要件に合わせて拡張しやすい形で提供します。

とくに `SymbolCaseEngine` + `CaseAnalysisOptions` を起点に使うことで、
実プロジェクトの命名検査・変換処理を段階的に強化できます。
