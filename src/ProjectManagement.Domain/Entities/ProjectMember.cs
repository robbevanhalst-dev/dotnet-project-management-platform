namespace ProjectManagement.Domain.Entities;

public class ProjectMember
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}


