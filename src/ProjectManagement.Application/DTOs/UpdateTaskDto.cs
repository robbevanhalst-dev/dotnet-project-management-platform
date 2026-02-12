using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.DTOs;

public class UpdateTaskDto
{
    [Required(ErrorMessage = "Task title is required")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Status is required")]
    [RegularExpression("^(ToDo|InProgress|Done)$", ErrorMessage = "Status must be ToDo, InProgress, or Done")]
    public string Status { get; set; } = "ToDo";

    [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Priority must be Low, Medium, or High")]
    public string? Priority { get; set; }

    public DateTime? DueDate { get; set; }
}
