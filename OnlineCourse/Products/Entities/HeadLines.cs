namespace OnlineCourse.Products.Entities;

public class HeadLines
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public byte Order { get; set; }
    public int DurationTime { get; set; }
    public int CourseSeasonId { get; set; }
    public CourseSeason CourseSeason { get; set; }
}
