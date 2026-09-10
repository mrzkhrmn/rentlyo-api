using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rentlyo.Application.DTOs.Auth;
using Rentlyo.Application.Interfaces;
using Rentlyo.Application.Options;
using Rentlyo.Domain.Constants;
using Rentlyo.Domain.Entities;
using Rentlyo.Domain.Enums;
using Rentlyo.Infrastructure.Persistence;
using Rentlyo.Shared.Exceptions;

namespace Rentlyo.Infrastructure.Auth;

public class AuthService(
    ApplicationDbContext db,
    IJwtTokenService jwtTokenService,
    IEmailSender emailSender,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRegister(request);

        var slug = request.Slug.Trim().ToLowerInvariant();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Tenants.AnyAsync(x => x.Slug == slug, cancellationToken))
        {
            throw new BusinessException("Company slug is already taken.");
        }

        if (await db.Users.AnyAsync(x => x.Email == email, cancellationToken))
        {
            throw new BusinessException("Email is already registered.");
        }

        var ownerRole = await db.Roles.FirstOrDefaultAsync(x => x.Id == SystemRoles.OwnerId, cancellationToken)
            ?? throw new BusinessException("Owner role is not configured.");

        var freePlan = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == SystemPlans.FreeId, cancellationToken)
            ?? throw new BusinessException("Free plan is not configured.");

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName.Trim(),
            Slug = slug,
            PlanId = freePlan.Id,
            Status = TenantStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Plan = freePlan
        };

        var settings = new TenantSettings
        {
            TenantId = tenant.Id,
            Timezone = "Europe/Istanbul",
            Currency = "TRY",
            Locale = "tr-TR",
            Tenant = tenant
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            RoleId = ownerRole.Id,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Role = ownerRole,
            Tenant = tenant
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        db.Tenants.Add(tenant);
        db.TenantSettings.Add(settings);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("Email and password are required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(x => x.Role)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        if (user.Tenant.Status is TenantStatus.Suspended or TenantStatus.Cancelled)
        {
            throw new ForbiddenException("Tenant is not active.");
        }

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAppException("Invalid email or password.");
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(Guid userId, string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            var tokens = await db.RefreshTokens
                .Where(x => x.UserId == userId && x.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in tokens)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }
        else
        {
            var hash = jwtTokenService.HashToken(refreshToken);
            var token = await db.RefreshTokens
                .FirstOrDefaultAsync(x => x.UserId == userId && x.TokenHash == hash && x.RevokedAt == null, cancellationToken);

            if (token is not null)
            {
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new ValidationException("Refresh token is required.");
        }

        var hash = jwtTokenService.HashToken(request.RefreshToken);
        var existing = await db.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(x => x.Role)
            .Include(x => x.User)
                .ThenInclude(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

        if (existing is null || !existing.IsActive || !existing.User.IsActive)
        {
            throw new UnauthorizedAppException("Invalid refresh token.");
        }

        existing.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(existing.User, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ValidationException("Email is required.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);

        // Always succeed to avoid email enumeration.
        if (user is null)
        {
            return;
        }

        var rawToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var reset = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = jwtTokenService.HashToken(rawToken),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        };

        db.PasswordResetTokens.Add(reset);
        await db.SaveChangesAsync(cancellationToken);

        await emailSender.SendAsync(
            user.Email,
            "Reset your Rentlyo password",
            $"Use this token to reset your password: {rawToken}",
            cancellationToken);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Token)
            || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            throw new ValidationException("Email, token and new password are required.");
        }

        if (request.NewPassword.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters.");
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken)
            ?? throw new BusinessException("Invalid reset token.");

        var hash = jwtTokenService.HashToken(request.Token);
        var resetToken = await db.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.TokenHash == hash)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (resetToken is null || !resetToken.IsActive)
        {
            throw new BusinessException("Invalid or expired reset token.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;

        var refreshTokens = await db.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in refreshTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<UserResponse> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(x => x.Role)
            .Include(x => x.Tenant)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return MapUser(user);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken cancellationToken)
    {
        if (user.Role is null)
        {
            await db.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);
        }

        if (user.Tenant is null)
        {
            await db.Entry(user).Reference(x => x.Tenant).LoadAsync(cancellationToken);
        }

        var (accessToken, expiresIn) = jwtTokenService.CreateAccessToken(user);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = jwtTokenService.HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = expiresIn,
            User = MapUser(user)
        };
    }

    private static UserResponse MapUser(User user) => new()
    {
        Id = user.Id,
        TenantId = user.TenantId,
        Email = user.Email,
        FullName = user.FullName,
        Role = user.Role.Name,
        TenantName = user.Tenant.Name,
        TenantSlug = user.Tenant.Slug
    };

    private static void ValidateRegister(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyName)
            || string.IsNullOrWhiteSpace(request.Slug)
            || string.IsNullOrWhiteSpace(request.FullName)
            || string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ValidationException("All registration fields are required.");
        }

        if (request.Password.Length < 8)
        {
            throw new ValidationException("Password must be at least 8 characters.");
        }

        if (!Regex.IsMatch(request.Slug.Trim(), @"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.IgnoreCase))
        {
            throw new ValidationException("Slug may only contain letters, numbers and hyphens.");
        }

        if (!request.Email.Contains('@'))
        {
            throw new ValidationException("Email is invalid.");
        }
    }
}
