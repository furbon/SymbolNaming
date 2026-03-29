namespace SymbolNaming.Engine;

/// <summary>
/// バルク処理の各要素に対する結果を保持します。
/// </summary>
public readonly struct BulkItemResult<T>
{
    private BulkItemResult(int index, bool isSuccess, T? value, Exception? error)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        if (isSuccess && error is not null)
        {
            throw new ArgumentException("Successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentNullException(nameof(error));
        }

        Index = index;
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    /// <summary>
    /// 入力順序におけるインデックスです。
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// 要素処理が成功したかを示します。
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// 成功時の値です。
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// 失敗時の例外です。
    /// </summary>
    public Exception? Error { get; }

    internal static BulkItemResult<T> Success(int index, T value)
    {
        return new BulkItemResult<T>(index, isSuccess: true, value, error: null);
    }

    internal static BulkItemResult<T> Failure(int index, Exception error)
    {
        return new BulkItemResult<T>(index, isSuccess: false, default, error);
    }
}
