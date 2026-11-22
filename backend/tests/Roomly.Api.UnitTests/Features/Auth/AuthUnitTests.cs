using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Roomly.Api.Common.Logging;
using Roomly.Api.Common.Time;
using Roomly.Api.Features.Auth;
using Roomly.Api.Features.Auth.Dtos;
using Roomly.Api.Features.Auth.Validators;

namespace Roomly.Api.UnitTests.Features.Auth;

public sealed class AuthUnitTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("berk@ornek.com", "b***@ornek.com")]
    [InlineData("a@b.co", "a***@b.co")]
    [InlineData("eposta-degil", "***")]
    [InlineData("", "(yok)")]
    [InlineData(null, "(yok)")]
    public void Email_IsMasked_SEC07(string? email, string expected) =>
        LogMask.Email(email).Should().Be(expected);

    [Fact]
    public void Token_IsAlwaysMasked_SEC07() => LogMask.Token("abc.def.ghi").Should().Be("***");

    [Theory]
    [InlineData("Berk", "berk@ornek.com", "Sifre123", true)]
    [InlineData(" B ", "berk@ornek.com", "Sifre123", false)]
    [InlineData("Berk", "berk@", "Sifre123", false)]
    [InlineData("Berk", "berk@ornek.com", "Sifre12", false)]
    [InlineData("Berk", "berk@ornek.com", "sifresifre", false)]
    [InlineData("Berk", "berk@ornek.com", "12345678", false)]
    public void RegisterValidator_AppliesRules_AUTH02(string name, string email, string password, bool valid) =>
        new RegisterRequestValidator().Validate(new RegisterRequest(name, email, password)).IsValid.Should().Be(valid);

    [Fact]
    public void RegisterValidator_RejectsNameLongerThan50_AUTH02() =>
        new RegisterRequestValidator().Validate(new RegisterRequest(new string('a', 51), "a@b.co", "Sifre123"))
            .IsValid.Should().BeFalse();

    [Fact]
    public void JwtIssuer_ExpiresFifteenMinutesAfterClockNow_AUTH07()
    {
        var options = Options.Create(new JwtOptions { SigningKey = new string('k', 40) });

        var token = new JwtTokenIssuer(options, new FixedClock(Now)).Issue(Guid.Empty);

        token.ExpiresAt.Should().Be(Now.AddMinutes(15));
        new JwtSecurityTokenHandler().ReadJwtToken(token.Value).ValidTo.Should().Be(Now.AddMinutes(15).UtcDateTime);
    }

    [Fact]
    public void RefreshTokenGenerator_Produces43CharTokenAnd32ByteHash_AUTH08()
    {
        var token = RefreshTokenGenerator.Generate();

        token.PlainText.Should().HaveLength(43);
        token.Hash.Should().HaveCount(32).And.Equal(RefreshTokenGenerator.Hash(token.PlainText));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData(JwtOptions.DevelopmentSigningKey, true)]
    public void JwtOptions_OutsideDevelopment_RejectsMissingShortOrDevKey_SEC07(string key, bool validInDevelopment)
    {
        var options = new JwtOptions { SigningKey = key };

        FluentActions.Invoking(() => options.EnsureValid(isDevelopment: false)).Should().Throw<InvalidOperationException>();
        if (validInDevelopment)
        {
            FluentActions.Invoking(() => options.EnsureValid(isDevelopment: true)).Should().NotThrow();
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow => now;
    }
}
