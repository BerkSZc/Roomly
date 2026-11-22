namespace Roomly.Api.Features.Groups;

/// <summary>
/// Ev ya da yurt grubu. Kapatılınca (<see cref="ClosedAt"/>) satır kalır, üyelikleri ve kodları silinir.
/// Üye sayısı saklanmaz; katılımda kilitli satır altında sayılır.
/// </summary>
public sealed class Group
{
    public const int NameMaxLength = 50;

    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required GroupType Type { get; set; }

    /// <summary>Yöneticiyi de sayar; sınırlar <see cref="GroupCapacityRules"/>.</summary>
    public required int Capacity { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ClosedAt { get; set; }
}
