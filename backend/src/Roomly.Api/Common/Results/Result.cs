namespace Roomly.Api.Common.Results;

/// <summary>Servislerin dönüş tipi: ya bir değer ya da bir <see cref="Error"/>.</summary>
public readonly record struct Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        Error = null;
    }

    private Result(Error error)
    {
        _value = default;
        Error = error;
    }

    public Error? Error { get; }

    public bool IsSuccess => Error is null;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Başarısız sonucun değeri okunamaz ({Error!.Code}).");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}

/// <summary>Değer taşımayan işlemler için: <c>Result&lt;Unit&gt;</c>.</summary>
public readonly record struct Unit
{
    public static readonly Unit Value = default;
}
