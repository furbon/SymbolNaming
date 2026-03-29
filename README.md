# SymbolNaming

`SymbolNaming` は、シンボル名（変数名・プロパティ名・フィールド名など）を対象に、
**トークン分割 / 命名スタイル判定 / スタイル変換 / 補助的な検査** を行う .NET ライブラリです。

## TL;DR

- 主用途: シンボル名の分割・判定・変換を一貫して扱う
- 基本API: まず `SymbolCaseEngine` を使う（`Analyze` / `TryAnalyze` / `Inspect` / `Convert`）
- 対象: C# で識別子として扱える命名（数字始まり・`-` 含みは対象外）
- 適用範囲: C#専用ではなく、同様の命名規則を持つ他言語にも適用可能
- 実装ターゲット: `netstandard2.0`（`.NET Framework 4.6.1+` / `.NET Core 2.0+` / `.NET 5+` で利用可能）
- 性能: `ReadOnlySpan<char>` オーバーロードを提供し、割り当てを抑えた利用が可能
- 並列利用: 共有インスタンスの読み取り中心利用を想定
- 拡張利用: `RuleBasedSymbolTokenizer` を直接構成する場合は `Freeze()` で設定確定後に使用
- 設定の目安: `PrefixProvider` には `m` / `s` / `k` / `str` のような接頭辞、`ProtectedWordProvider` には `iOS` のような保護語を登録

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

## ドキュメント更新ルール（開発向け）

- 機能追加 PR では、実装変更にあわせて `README.md` と `Instructions.md` を同時更新します。
- API 変更時は、利用者影響（移行要否）を `README.md` に明記します。

---

## 対応する命名スタイル

`CaseStyle` では次のスタイルをサポートします。

- `PascalCase`
- `CamelCase`
- `UpperSnakeCase`（例: `User_Name`）
- `LowerSnakeCase`（例: `user_name`）
- `ScreamingSnakeCase`（例: `USER_NAME`）
- `Unknown`（判定不能）

---

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

主な取得項目:

- `Prefix` / `SymbolNameWithoutPrefix`
- `LeadingUnderscoreCount` / `TrailingUnderscoreCount`
- `Warnings`（注意すべき分割パターン）
- `CompositePattern`（複合サフィックス一致時）

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

ルールは登録順に実行されるため、並列利用時も同一入力に対して決定的な順序で `Warnings` を取得できます。

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

加えて、Roslyn アナライザ実装のような高頻度かつ並列実行のシナリオでも利用できるよう、
`Analyze` / `TryAnalyze` / `Inspect` には `ReadOnlySpan<char>` を受け取るオーバーロードを用意しています。

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
```

- CPU・メモリ効率を重視し、不要な割り当てを抑える設計
- 並列実行時は、インスタンスを読み取り中心で共有しやすい構成

なお、本ライブラリはアナライザ専用ライブラリではありません。
通常のアプリケーションコードやツール実装でも同じ API を利用できます。

### 5. トークナイザーを直接構成する場合（拡張利用）

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
