namespace OnlineCourse.Entities;

public class PhysicalProduct : Product
{
    public string[] ImagesPath { get; set; }
    public int StockQuantity { get; set; }
}