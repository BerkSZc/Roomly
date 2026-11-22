namespace Roomly.Api.Features.Groups;

/// <summary>
/// BND-02: grup türüne göre kapasite sınırları (yönetici dahil, sınırlar dahil). Tek kaynak:
/// hem <c>ck_groups_capacity_by_type</c> kısıtı hem UC2 validator'ı buradan okur.
/// Sınırı değiştirmek bir migration ister.
/// </summary>
public static class GroupCapacityRules
{
    public static CapacityRange RangeFor(GroupType type) => type switch
    {
        GroupType.Ev => new CapacityRange(2, 20),
        GroupType.Yurt => new CapacityRange(2, 6),
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Bilinmeyen grup türü."),
    };

    public static bool IsValid(GroupType type, int capacity) => RangeFor(type).Contains(capacity);

    /// <summary>Check kısıtının SQL metni, ör. <c>(type = 'Ev' AND capacity BETWEEN 2 AND 20) OR ...</c>.</summary>
    public static string CheckConstraintSql() => string.Join(
        " OR ",
        Enum.GetValues<GroupType>().Select(type =>
        {
            var range = RangeFor(type);
            return $"(type = '{type}' AND capacity BETWEEN {range.Min} AND {range.Max})";
        }));
}

public readonly record struct CapacityRange(int Min, int Max)
{
    public bool Contains(int capacity) => capacity >= Min && capacity <= Max;
}
