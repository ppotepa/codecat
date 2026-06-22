namespace Codecat.Scanning;

using System.Collections.Immutable;

public sealed class Result<T, TError>
{
    private Result(T? value, ImmutableArray<TError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }
    public ImmutableArray<TError> Errors { get; }
    public bool IsSuccess => Errors.IsDefaultOrEmpty && Value is not null;

    public static Result<T, TError> Success(T value) => new(value, ImmutableArray<TError>.Empty);

    public static Result<T, TError> Failure(TError error) => new(default, ImmutableArray.Create(error));

    public static Result<T, TError> Failure(IEnumerable<TError> errors) => new(default, errors.ToImmutableArray());
}
