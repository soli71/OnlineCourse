namespace OnlineCourse.Entities;

public class OrderDetails
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public decimal Price { get; set; }
    public string License { get; set; }
    public string Key { get; set; }
}
