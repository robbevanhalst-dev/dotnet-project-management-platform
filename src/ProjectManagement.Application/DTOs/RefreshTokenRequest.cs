using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
