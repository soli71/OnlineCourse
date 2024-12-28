namespace OnlineCourse.Entities;

public class Course : SEO
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
    public bool IsPublish { get; set; }
    public ICollection<CourseSeason> CourseSeasons { get; set; }
}