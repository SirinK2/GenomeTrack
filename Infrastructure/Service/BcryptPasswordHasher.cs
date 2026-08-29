using GenomeTrack.Application.Services.Interfaces;

namespace GenomeTrack.Infrastructure.Service;

public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hash)
    {
        // A malformed stored hash must read as "wrong password", not as a 500. Otherwise a
        // corrupted row turns into an outage on one account and a confusing error for the user.
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
