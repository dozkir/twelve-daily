namespace TwelveDaily.Core.Application.Common;

public sealed class ApplicationResult
{
    public bool Success { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private ApplicationResult(bool success, IReadOnlyCollection<string> errors)
    {
        Success = success;
        Errors = errors;
    }

    public static ApplicationResult Ok() => new(true, []);
    public static ApplicationResult Fail(IReadOnlyCollection<string> errors) => new(false, errors);
}

public sealed class ApplicationResult<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public IReadOnlyCollection<string> Errors { get; }

    private ApplicationResult(bool success, T? value, IReadOnlyCollection<string> errors)
    {
        Success = success;
        Value = value;
        Errors = errors;
    }

    public static ApplicationResult<T> Ok(T? value) => new(true, value, []);
    public static ApplicationResult<T> Fail(IReadOnlyCollection<string> errors) => new(false, default, errors);
}