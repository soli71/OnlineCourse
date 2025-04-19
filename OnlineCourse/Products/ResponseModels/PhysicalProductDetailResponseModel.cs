namespace OnlineCourse.Products.ResponseModels;

/// <summary>
/// DTO for retrieving detailed information of a physical product
/// </summary>
public record PhysicalProductDetailResponseModel(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsPublish
);