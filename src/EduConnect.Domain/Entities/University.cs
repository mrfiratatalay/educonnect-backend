using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class University : AuditableEntity
{
    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}
