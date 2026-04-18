using EduConnect.Domain.Common;

namespace EduConnect.Domain.Entities;

public sealed class ProductImage : AuditableEntity
{
    public Guid ProductId { get; set; }

    public string Url { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public string? EmbeddingJson { get; set; }

    public Product Product { get; set; } = null!;
}
