namespace OnlineCourse.Products.ResponseModels.Site;

public class PhysicalProductDetailResponseModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public string Description { get; set; }
    public List<string> ImagesPath { get; set; } = new();

    public PhysicalProductDetailResponseModel(int id, string name, decimal price, int? stockQuantity, string description, string defaultImageUrl, List<string> imagesPath)
    {
        Id = id;
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
        ImagesPath.Add(defaultImageUrl);
        ImagesPath.AddRange(imagesPath);
        Description = description;
    }
}