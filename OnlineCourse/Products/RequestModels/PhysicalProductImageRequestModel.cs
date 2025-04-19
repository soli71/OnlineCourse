using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Products.RequestModels;

public class PhysicalProductImageRequestModel : IValidatableObject
{
    [Required]
    public IFormFile Image { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Image == null)
        {
            yield return new ValidationResult("عکس محصول الزامی است", new[] { nameof(Image) });
        }
        else if (Image.Length > 5 * 1024 * 1024)
        {
            yield return new ValidationResult("حجم عکس باید کمتر از 5 مگابایت باشد", new[] { nameof(Image) });
        }
        else if (!Image.ContentType.StartsWith("image/"))
        {
            yield return new ValidationResult("فرمت فایل باید تصویر باشد", new[] { nameof(Image) });
        }
    }
}