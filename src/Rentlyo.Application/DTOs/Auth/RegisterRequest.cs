namespace Rentlyo.Application.DTOs.Auth;

public class RegisterRequest
{
    public string CompanyName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
