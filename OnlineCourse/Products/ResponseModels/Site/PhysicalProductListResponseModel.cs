namespace OnlineCourse.Products.ResponseModels.Site;

public class PhysicalProductListResponseModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int? StockQuantity { get; set; }
    public string DefaultImageUrl { get; set; }
    public string Slug { get; set; }

    public PhysicalProductListResponseModel(int id, string name, decimal price, int? stockQuantity, string defaultImageUrl, string slug)
    {
        Id = id;
        Name = name;
        Price = price;
        StockQuantity = stockQuantity;
        DefaultImageUrl = defaultImageUrl;
        Slug = slug;
    }
}