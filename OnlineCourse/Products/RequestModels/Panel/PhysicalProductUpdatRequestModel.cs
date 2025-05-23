﻿using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Products.RequestModels.Panel;

/// <summary>
/// DTO for updating an existing physical product
/// </summary>
public class PhysicalProductUpdatRequestModel : IValidatableObject
{
    [Required]
    public string Name { get; init; }

    public string Description { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    public bool IsPublish { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            yield return new ValidationResult("Name is required.", new[] { nameof(Name) });
        }
        if (Price < 0)
        {
            yield return new ValidationResult("Price must be a positive number.", new[] { nameof(Price) });
        }
    }
}