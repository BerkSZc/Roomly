using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Roomly.Api.Features.Groups;

namespace Roomly.Api.UnitTests.Features.Groups;

public sealed class InviteCodeHasherTests
{
    private const string Canonical = "DEMO-ROOM-2026";

    [Theory]
    [InlineData("DEMO-ROOM-2026")]
    [InlineData("demo-room-2026")]
    [InlineData("DEMO ROOM 2026")]
    [InlineData("DEMOROOM2026")]
    [InlineData("demo\troom\t2026")]
    [InlineData("DEMO ROOM 2026")]
    [InlineData("DEMO–ROOM–2026")]
    [InlineData("  Demo - Room - 2026  ")]
    public void Hash_SpellingVariants_ProduceSameHash_DM07(string variant)
    {
        InviteCodeHasher.Hash(variant).Should().Equal(InviteCodeHasher.Hash(Canonical));
    }

    [Fact]
    public void Hash_IsSha256OfNormalizedUtf8_DM07()
    {
        var expected = SHA256.HashData(Encoding.UTF8.GetBytes("DEMOROOM2026"));

        var hash = InviteCodeHasher.Hash(Canonical);

        hash.Should().HaveCount(InviteCodeRules.HashLength).And.Equal(expected);
    }

    [Fact]
    public void Hash_DifferentCodes_ProduceDifferentHashes_DM07()
    {
        InviteCodeHasher.Hash("ABCD-2345").Should().NotEqual(InviteCodeHasher.Hash("ABCD-2346"));
    }

    [Fact]
    public void Normalize_RemovesWhitespaceAndDashes_AndUppercases_DM07()
    {
        InviteCodeHasher.Normalize(" de\tmo -ro–om 2026\n").Should().Be("DEMOROOM2026");
    }
}
