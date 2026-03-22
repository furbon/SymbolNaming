namespace SymbolNaming;

/// <summary>
/// シンボル名の命名スタイルを表します。
/// </summary>
public enum CaseStyle
{
    /// <summary>
    /// 判定不能または未指定のスタイルです。
    /// </summary>
    Unknown,

    /// <summary>
    /// <c>PascalCase</c> 形式です。
    /// </summary>
    PascalCase,

    /// <summary>
    /// <c>camelCase</c> 形式です。
    /// </summary>
    CamelCase,

    /// <summary>
    /// <c>Upper_Snake_Case</c> 形式です。
    /// </summary>
    UpperSnakeCase,

    /// <summary>
    /// <c>lower_snake_case</c> 形式です。
    /// </summary>
    LowerSnakeCase,

    /// <summary>
    /// <c>SCREAMING_SNAKE_CASE</c> 形式です。
    /// </summary>
    ScreamingSnakeCase,
}
