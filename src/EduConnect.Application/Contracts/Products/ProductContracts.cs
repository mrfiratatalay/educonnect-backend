using System.ComponentModel.DataAnnotations;
using EduConnect.Domain.Enums;

namespace EduConnect.Application.Contracts.Products;

public sealed class ProductFilterRequest
{
    public Guid? CategoryId { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public ProductCondition? Condition { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 12;
}

public class CreateProductRequest
{
    [Required, StringLength(200, MinimumLength = 3)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(3000, MinimumLength = 10)]
    public string Description { get; init; } = string.Empty;

    [Range(0, 10_000_000)]
    public decimal Price { get; init; }

    public Guid? CategoryId { get; init; }

    [Required]
    public ProductCondition Condition { get; init; }

    [Required, StringLength(120)]
    public string City { get; init; } = string.Empty;

    public bool IsNegotiable { get; init; }

    public IReadOnlyCollection<string> ImageUrls { get; init; } = [];
}

public sealed class UpdateProductRequest : CreateProductRequest;

public sealed class ProductResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public Guid? CategoryId { get; init; }

    public string? CategoryName { get; init; }

    public Guid SellerId { get; init; }

    public string SellerName { get; init; } = string.Empty;

    public ProductCondition Condition { get; init; }

    public bool IsActive { get; init; }

    public string City { get; init; } = string.Empty;

    public bool IsNegotiable { get; init; }

    public IReadOnlyCollection<string> ImageUrls { get; init; } = [];

    public DateTime CreatedAtUtc { get; init; }
}
