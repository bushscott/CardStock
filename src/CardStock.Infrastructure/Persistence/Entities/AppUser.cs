namespace CardStock.Infrastructure.Persistence.Entities;

/// <summary>
/// An account. Email is the natural key and is unique. Open signup (D-011),
/// email and password only (D-034); the password policy is exactly one rule,
/// minimum length 12 with no complexity requirement (docs/screens/account.md:139).
/// </summary>
public class AppUser
{
    public long Id { get; set; }

    public required string Email { get; set; }

    /// <summary>Produced by ASP.NET Core's PasswordHasher. Never a raw password.</summary>
    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Null until the verification link is followed.</summary>
    public DateTimeOffset? EmailVerifiedAt { get; set; }
}
