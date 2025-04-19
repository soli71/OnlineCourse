using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Products.RequestModels;

/// <summary>
/// DTO for creating a new physical product
/// </summary>
public class PhysicalProductCreateRequestModel : IValidatableObject
{
    [Required]
    public string Name { get; init; }

    public string Description { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    [Required]
    public IFormFile DefaultImage { get; init; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }

    public bool IsPublish { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DefaultImage.Length > 5 * 1024 * 1024) // 5 MB
        {
            yield return new ValidationResult("Each image must be less than 5 MB.", new[] { nameof(DefaultImage) });
        }
        if (!DefaultImage.ContentType.StartsWith("image/"))
        {
            yield return new ValidationResult("Only image files are allowed.", new[] { nameof(DefaultImage) });
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
        }
        if (Price <= 0)
        {
            yield return new ValidationResult("Price must be greater than zero.", new[] { nameof(Price) });
        }
        if (StockQuantity < 0)
        {
            yield return new ValidationResult("Stock quantity cannot be negative.", new[] { nameof(StockQuantity) });
        }
    }
}