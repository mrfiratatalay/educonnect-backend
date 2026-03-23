using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class Discount : AuditableEntity
{
    public string BusinessName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal DiscountRate { get; set; }

    public string DiscountCode { get; set; } = string.Empty;

    public string? LogoUrl { get; set; }

    public DateTime ValidUntilUtc { get; set; }

    public bool IsActive { get; set; } = true;
}
