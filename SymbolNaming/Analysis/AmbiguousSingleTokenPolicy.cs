namespace SymbolNaming.Analysis;

/// <summary>
/// セパレーターなし単一語で複数スタイルが成立する場合の解決方針です。
/// </summary>
public enum AmbiguousSingleTokenPolicy
{
    /// <summary>
    /// 先頭文字を優先して <c>PascalCase</c> / <c>camelCase</c> を選択します。
    /// </summary>
    PreferPascalOrCamel,

    /// <summary>
    /// スネークケース系（<c>Upper_Snake_Case</c> / <c>lower_snake_case</c> / <c>SCREAMING_SNAKE_CASE</c>）を優先します。
    /// </summary>
    PreferSnakeCase,

    /// <summary>
    /// 曖昧時は判定不能として扱います。
    /// </summary>
    ReturnUnknown,

    /// <summary>
    /// カスタム解決関数を使用して最終スタイルを決定します。
    /// </summary>
    UseCustomResolver,
}
