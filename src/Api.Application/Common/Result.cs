namespace Api.Application.Common;

/// <summary>Why a use case didn't succeed. The API turns each kind into an HTTP status.</summary>
public enum FailureKind
{
    /// <summary>The input broke a rule: 400, with an failure per field.</summary>
    Validation,
    /// <summary>The thing asked for doesn't exist: 404.</summary>
    NotFound,
    /// <summary>The input clashes with what is stored, such as a taken email: 409.</summary>
    Conflict,
}

/// <summary>A failed use case: what went wrong, a title for users and, optionally, errors per field.</summary>
/// <param name="Kind">What went wrong.</param>
/// <param name="Title">A sentence to show the caller.</param>
/// <param name="Fields">Errors per field (camelCase keys), so a form can show each next to its field.</param>
public sealed record Failure(FailureKind Kind, string Title, IReadOnlyDictionary<string, string[]>? Fields = null);

/// <summary>The outcome of a use case that returns nothing: success, or an <see cref="Common.Failure"/>.</summary>
public class Result
{
    protected Result(Failure? failure) => Failure = failure;

    /// <summary>Null on success.</summary>
    public Failure? Failure { get; }

    public bool Succeeded => Failure is null;

    public static Result Success { get; } = new(null);

    public static implicit operator Result(Failure failure) => new(failure);
}

/// <summary>The outcome of a use case: a value, or an <see cref="Common.Failure"/>.</summary>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(null) => _value = value;

    private Result(Failure failure) : base(failure) { }

    /// <summary>The value. Only read it when <see cref="Result.Succeeded"/>.</summary>
    public T Value => Succeeded ? _value! : throw new InvalidOperationException("The use case failed: read Failure instead.");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Failure failure) => new(failure);
}
