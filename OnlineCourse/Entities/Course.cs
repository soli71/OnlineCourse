namespace OnlineCourse.Entities;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int DurationTime { get; set; }
    public string ImageFileName { get; set; }
    public string SpotPlayerCourseId { get; set; }
}
