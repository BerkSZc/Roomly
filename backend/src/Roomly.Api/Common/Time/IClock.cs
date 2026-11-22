namespace Roomly.Api.Common.Time;

/// <summary>
/// Şimdiki zamanı okumanın tek yolu (BND-03). Kodda <c>DateTime.Now</c> ya da
/// <c>DateTimeOffset.UtcNow</c> kullanmayın; testlerde sahte saat verilir.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
