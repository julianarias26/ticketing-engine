namespace TicketingEngine.Application.Common;

public sealed class Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess { get; }
    private Result(T value)      { Value = value; IsSuccess = true; }
    private Result(string error) { Error = error; IsSuccess = false; }
    public static Result<T> Success(T value)  => new(value);
    public static Result<T> Failure(string e) => new(e);
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error!);
}

public sealed class Result
{
    public string? Error { get; }
    public bool IsSuccess { get; }
    private Result()           { IsSuccess = true; }
    private Result(string err) { Error = err; IsSuccess = false; }
    public static Result Success()           => new();
    public static Result Failure(string err) => new(err);
}
