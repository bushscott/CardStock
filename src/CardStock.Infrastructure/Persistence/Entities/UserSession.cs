namespace CardStock.Infrastructure.Persistence.Entities;

/// <summary>
/// One signed-in session, referenced by the HttpOnly cookie (ADR-0002).
///
/// The session lives here rather than as claims inside the cookie so that
/// signing out, "sign out everywhere", and account deletion take effect on the
/// next request instead of whenever the cookie happens to expire. That matters
/// because deletion is immediate and permanent while backups stay deferred
/// (D-069).
///
/// This is still a stateless API in the sense D-063 means: no session is held
/// in server memory, so a deploy disconnects nobody.
/// </summary>
public class UserSession
{
    /// <summary>The opaque key carried in the cookie. Generated, never guessable.</summary>
    public required string Id { get; set; }

    public long UserId { get; set; }

    public AppUser? User { get; set; }

    /// <summary>The serialized authentication ticket, as ITicketStore supplies it.</summary>
    public required byte[] Payload { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
