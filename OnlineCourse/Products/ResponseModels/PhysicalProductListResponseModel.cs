namespace OnlineCourse.Products.ResponseModels;

/// <summary>
/// DTO for listing physical products in admin panel
/// </summary>
public record PhysicalProductListResponseModel(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsPublish
);
