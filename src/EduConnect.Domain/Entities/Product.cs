using EduConnect.Domain.Common;
using EduConnect.Domain.Enums;

namespace EduConnect.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public Guid? CategoryId { get; set; }

    public Guid SellerId { get; set; }

    public ProductCondition Condition { get; set; } = ProductCondition.Good;

    public bool IsActive { get; set; } = true;

    public string City { get; set; } = string.Empty;

    public bool IsNegotiable { get; set; }

    public Category? Category { get; set; }

    public User Seller { get; set; } = null!;

    public ICollection<ProductImage> Images { get; set; } = [];
}
