using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs;

public class UpdateUserRoleDto
{
    [Required(ErrorMessage = "Role is required")]
    [RegularExpression("^(User|Manager|Admin)$", ErrorMessage = "Role must be User, Manager, or Admin")]
    public string Role { get; set; } = string.Empty;
}
