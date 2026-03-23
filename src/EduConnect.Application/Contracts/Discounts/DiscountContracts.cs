using System.ComponentModel.DataAnnotations;

namespace EduConnect.Application.Contracts.Discounts;

public sealed class CreateDiscountRequest
{
    [Required, StringLength(150)]
    public string BusinessName { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Title { get; init; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Range(0, 100)]
    public decimal DiscountRate { get; init; }

    [Required, StringLength(50)]
    public string DiscountCode { get; init; } = string.Empty;

    [Url]
    public string? LogoUrl { get; init; }

    public DateTime ValidUntilUtc { get; init; }
}

public sealed class DiscountResponse
{
    public Guid Id { get; init; }

    public string BusinessName { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal DiscountRate { get; init; }

    public string DiscountCode { get; init; } = string.Empty;

    public string? LogoUrl { get; init; }

    public DateTime ValidUntilUtc { get; init; }

    public bool IsActive { get; init; }
}
