using FluentAssertions;
using Npgsql;

namespace Roomly.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Kısıt testleri için yardımcılar. Her satır kendi context'iyle yazılır, böylece hata EF'in
/// izleyicisinden değil veritabanından gelir ve <see cref="PostgresException"/> olarak doğrulanır.
/// </summary>
internal static class DbAssert
{
    public const string UniqueViolation = "23505";
    public const string ForeignKeyViolation = "23503";
    public const string CheckViolation = "23514";
    public const string StringTooLong = "22001";

    /// <summary>Satırları sırayla, her biri ayrı context ve ayrı <c>SaveChanges</c> ile yazar.</summary>
    public static async Task SeedAsync(this PostgresFixture fixture, params object[] entities)
    {
        foreach (var entity in entities)
        {
            await using var db = fixture.CreateContext();
            db.Add(entity);
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Satırı yazmayı dener; veritabanının reddettiğini doğrular ve hatayı döner.</summary>
    public static Task<PostgresException> InsertRejectedAsync(this PostgresFixture fixture, object entity) =>
        RejectedAsync(() => fixture.SeedAsync(entity));

    /// <summary>İşlemin bir <see cref="PostgresException"/> ile (doğrudan ya da sarılı) başarısız olduğunu doğrular.</summary>
    public static async Task<PostgresException> RejectedAsync(Func<Task> action)
    {
        var exception = await Record.ExceptionAsync(action);

        exception.Should().NotBeNull("veritabanı bu yazmayı reddetmeliydi");
        var postgres = exception as PostgresException ?? exception!.InnerException as PostgresException;
        postgres.Should().NotBeNull($"hata PostgreSQL'den gelmeli, gelen: {exception}");
        return postgres!;
    }

    public static void ShouldViolate(this PostgresException exception, string sqlState, string constraintName)
    {
        exception.SqlState.Should().Be(sqlState);
        exception.ConstraintName.Should().Be(constraintName);
    }
}
