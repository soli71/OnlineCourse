namespace OnlineCourse.Products.Entities;

public class CourseSeason
{
    public int Id { get; set; }
    public string Name { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public byte Order { get; set; }
    public ICollection<HeadLines> HeadLines { get; set; }
}
