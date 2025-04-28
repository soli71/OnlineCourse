namespace OnlineCourse.Products.Entities;

public class PhysicalProduct : Product
{
    public string[] ImagesPath { get; set; }
    public int StockQuantity { get; private set; }

    public void SetStockQuantity(int quantity)
    {
        if (quantity <= 0)
        { }
        else
        {
            StockQuantity = quantity;
        }
    }

    public void DecreaseStockQuantity(int quantity)
    {
        if (quantity <= 0)
        { return; }
        if (quantity > StockQuantity)
        {
            throw new InvalidOperationException("Insufficient stock quantity.");
        }
        else
        {
            StockQuantity -= quantity;
        }
    }
}