using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectManagement.Domain.Entities;

public class ProjectMember
{
    public Guid UserId { get; set; }
    public Guid ProjectId { get; set; }

    public string Role { get; set; } = "Member";
}

