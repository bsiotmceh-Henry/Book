namespace Book.Services;

/// <summary>業務規則結果（以 Result 回傳，不以例外控制流程）。</summary>
public record Result(bool Success, string Message)
{
    public static Result Ok(string message = "") => new(true, message);
    public static Result Fail(string message) => new(false, message);
}

public record Result<T>(bool Success, string Message, T? Value)
{
    public static Result<T> Ok(T value) => new(true, "", value);
    public static Result<T> Fail(string message) => new(false, message, default);
}
