using TutoringBackend.Api.Models;

namespace TutoringBackend.Api.Services;

public interface ITokenService
{
    string CreateAccessToken(ApplicationUser user, IList<string> roles);

    ///Zwraca (rawToken, hashToStore, expiresAtUtc). Surowy token idzie do klienta, hash do bazy.
    (string rawToken, string hash, DateTime expiresAtUtc) CreateRefreshToken();

    string HashRefreshToken(string rawToken);
}
