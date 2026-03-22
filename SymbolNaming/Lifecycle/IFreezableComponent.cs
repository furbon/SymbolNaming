namespace SymbolNaming.Lifecycle;

/// <summary>
/// 初期構築後に状態を凍結できるコンポーネントを表します。
/// </summary>
public interface IFreezableComponent
{
    /// <summary>
    /// 状態が凍結済みかどうかを取得します。
    /// </summary>
    bool IsFrozen { get; }

    /// <summary>
    /// 状態を凍結し、以後の変更を禁止します。
    /// </summary>
    void Freeze();
}
