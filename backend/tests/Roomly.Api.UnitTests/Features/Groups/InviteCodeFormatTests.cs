using FluentAssertions;
using Roomly.Api.Features.Groups;

namespace Roomly.Api.UnitTests.Features.Groups;

public sealed class InviteCodeFormatTests
{
    [Theory]
    [InlineData("DEMO-ROOM-2026", true)]
    [InlineData("demo room 2026", true)]
    [InlineData("ABCD-EFGH-JKLM", true)]
    [InlineData("ABCDEFG", false)]
    [InlineData("ABCDEFGH", true)]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ234567", true)]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ2345678", false)]
    [InlineData("KOD!KOD!KOD", false)]
    [InlineData("ÇOKGÜZELKOD1", false)]
    public void IsWellFormed_AfterNormalize_ChecksLengthAndAlphabet_JOIN02(string code, bool expected) =>
        InviteCodeFormat.IsWellFormed(InviteCodeHasher.Normalize(code)).Should().Be(expected);
}
