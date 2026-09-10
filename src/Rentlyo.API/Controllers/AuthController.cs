using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rentlyo.Application.DTOs.Auth;
using Rentlyo.Application.Interfaces;
using Rentlyo.Shared.Responses;

namespace Rentlyo.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var data = await authService.RegisterAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Success(data, "Registration successful."));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var data = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Success(data, "Login successful."));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var data = await authService.RefreshAsync(request, cancellationToken);
        return Ok(ApiResponse<AuthResponse>.Success(data));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Logout(
        [FromBody] RefreshTokenRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        await authService.LogoutAsync(userId, request?.RefreshToken, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { }, "Logged out."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Success(
            new { },
            "If the email exists, a reset token has been sent."));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Success(new { }, "Password has been reset."));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Me(CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var data = await authService.GetMeAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Success(data));
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue("sub")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (value is null || !Guid.TryParse(value, out var userId))
        {
            throw new Rentlyo.Shared.Exceptions.UnauthorizedAppException("Invalid user token.");
        }

        return userId;
    }
}
