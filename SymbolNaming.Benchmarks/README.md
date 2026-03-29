# SymbolNaming.Benchmarks

`BenchmarkDotNet` を使った継続的ベンチマーク用プロジェクトです。

## 実行

```powershell
dotnet run --project .\SymbolNaming.Benchmarks\SymbolNaming.Benchmarks.csproj -c Release
```

## 計測対象

- `EngineBenchmarks.Analyze`
- `EngineBenchmarks.Inspect`
- `EngineBenchmarks.ConvertToScreamingSnake`

`[Params]` で代表的なシンボル（PascalCase / Prefix付き / Composite suffix / acronym / decorated snake）を切り替えて計測します。

## 結果の見方

- 実行後、`BenchmarkDotNet.Artifacts\results` にレポートが出力されます。
- 大きな仕様変更・最適化の前後比較では、このレポートを保存して差分確認してください。
