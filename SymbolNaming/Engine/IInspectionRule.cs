using SymbolNaming.Analysis;
using SymbolNaming.Tokens;

namespace SymbolNaming.Engine;

/// <summary>
/// Inspect 時の警告判定ルールを表します。
/// </summary>
public interface IInspectionRule
{
    /// <summary>
    /// 入力に対して警告を 1 件生成できる場合に <see langword="true"/> を返します。
    /// </summary>
    bool TryCreateWarning(ReadOnlySpan<char> source, TokenList tokens, CaseClassificationResult classification, out SymbolInspectionWarning warning);
}
