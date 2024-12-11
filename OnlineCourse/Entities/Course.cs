namespace OnlineCourse.Entities;

public class Cart
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public CartStatus Status { get; set; }  

    public List<CartItem> CartItems { get; set; }
}
public class CartItem
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public decimal Price { get; set; }
    public Cart Cart { get; set; }
    public int CartId { get; set; }
    public bool IsDelete { get; set; }
    public string Message { get; set; }

}
public enum CartStatus
{
    Active,
    Close
}
public class Course
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int DurationTime { get; set; }
    public string ImageFileName { get; set; }
    public string SpotPlayerCourseId { get; set; }
    public string PreviewVideoName { get; set; }
    public byte Limit { get; set; }
    public int FakeStudentsCount { get; set; }
}