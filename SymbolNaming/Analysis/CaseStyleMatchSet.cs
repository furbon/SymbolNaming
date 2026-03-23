namespace SymbolNaming.Analysis;

/// <summary>
/// 候補となる命名スタイルの集合を表します。
/// </summary>
public readonly struct CaseStyleMatchSet
{
    /// <summary>
    /// 候補集合を初期化します。
    /// </summary>
    public CaseStyleMatchSet(bool pascalCase, bool camelCase, bool upperSnakeCase, bool lowerSnakeCase, bool screamingSnakeCase)
    {
        PascalCase = pascalCase;
        CamelCase = camelCase;
        UpperSnakeCase = upperSnakeCase;
        LowerSnakeCase = lowerSnakeCase;
        ScreamingSnakeCase = screamingSnakeCase;
    }

    /// <summary>
    /// <c>PascalCase</c> が候補かどうかを示します。
    /// </summary>
    public bool PascalCase { get; }

    /// <summary>
    /// <c>camelCase</c> が候補かどうかを示します。
    /// </summary>
    public bool CamelCase { get; }

    /// <summary>
    /// <c>Upper_Snake_Case</c> が候補かどうかを示します。
    /// </summary>
    public bool UpperSnakeCase { get; }

    /// <summary>
    /// <c>lower_snake_case</c> が候補かどうかを示します。
    /// </summary>
    public bool LowerSnakeCase { get; }

    /// <summary>
    /// <c>SCREAMING_SNAKE_CASE</c> が候補かどうかを示します。
    /// </summary>
    public bool ScreamingSnakeCase { get; }

    /// <summary>
    /// 候補が 1 つ以上存在するかどうかを示します。
    /// </summary>
    public bool HasAny => PascalCase || CamelCase || UpperSnakeCase || LowerSnakeCase || ScreamingSnakeCase;

    /// <summary>
    /// 指定スタイルが候補に含まれるかどうかを返します。
    /// </summary>
    public bool IsMatch(CaseStyle style)
    {
        switch (style)
        {
            case CaseStyle.PascalCase:
                return PascalCase;
            case CaseStyle.CamelCase:
                return CamelCase;
            case CaseStyle.UpperSnakeCase:
                return UpperSnakeCase;
            case CaseStyle.LowerSnakeCase:
                return LowerSnakeCase;
            case CaseStyle.ScreamingSnakeCase:
                return ScreamingSnakeCase;
            default:
                return false;
        }
    }

    internal int Count
    {
        get
        {
            var count = 0;
            if (PascalCase)
            {
                count++;
            }

            if (CamelCase)
            {
                count++;
            }

            if (UpperSnakeCase)
            {
                count++;
            }

            if (LowerSnakeCase)
            {
                count++;
            }

            if (ScreamingSnakeCase)
            {
                count++;
            }

            return count;
        }
    }
}
