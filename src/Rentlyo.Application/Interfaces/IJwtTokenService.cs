using System.Security.Claims;
using Rentlyo.Domain.Entities;

namespace Rentlyo.Application.Interfaces;

public interface IJwtTokenService
{
    (string Token, int ExpiresInSeconds) CreateAccessToken(User user);
    (string Token, int ExpiresInSeconds) CreatePlatformAccessToken(PlatformUser user);
    string CreateRefreshToken();
    string HashToken(string token);
}
