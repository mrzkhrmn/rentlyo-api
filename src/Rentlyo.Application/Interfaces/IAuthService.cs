using Rentlyo.Application.DTOs.Auth;

namespace Rentlyo.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken = default);
}
