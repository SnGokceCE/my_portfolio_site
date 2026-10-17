namespace Portfolio.Application.Common.Models;

/// <summary>Bir doğrulama ya da iş kuralı hatası. <see cref="Field"/> formdaki alanı işaret eder.</summary>
public sealed record Error(string Message, string? Field = null);

public enum ResultStatus
{
    Ok,
    Invalid,
    NotFound
}

/// <summary>
/// Servislerin sonucu. Beklenen hatalar (geçersiz giriş, bulunamayan kayıt) istisna
/// fırlatmak yerine bu tiple döndürülür; böylece controller akışı sade kalır.
/// </summary>
public class Result
{
    protected Result(ResultStatus status, IReadOnlyList<Error> errors)
    {
        Status = status;
        Errors = errors;
    }

    public ResultStatus Status { get; }

    public IReadOnlyList<Error> Errors { get; }

    public bool Succeeded => Status == ResultStatus.Ok;

    public bool IsNotFound => Status == ResultStatus.NotFound;

    public static Result Success() => new(ResultStatus.Ok, []);

    public static Result NotFound() => new(ResultStatus.NotFound, []);

    public static Result Invalid(IReadOnlyList<Error> errors) => new(ResultStatus.Invalid, errors);

    public static Result Invalid(string message, string? field = null) => Invalid([new Error(message, field)]);

    public static Result<T> Success<T>(T value) => new(ResultStatus.Ok, value, []);

    public static Result<T> NotFound<T>() => new(ResultStatus.NotFound, default, []);

    public static Result<T> Invalid<T>(IReadOnlyList<Error> errors) => new(ResultStatus.Invalid, default, errors);
}

public sealed class Result<T> : Result
{
    internal Result(ResultStatus status, T? value, IReadOnlyList<Error> errors) : base(status, errors)
    {
        Value = value;
    }

    public T? Value { get; }
}
