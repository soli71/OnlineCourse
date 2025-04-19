namespace OnlineCourse.Products.Entities;

public class Course : Product
{
    public int DurationTime { get; set; }
    public string SpotPlayerCourseId { get; set; }
    public string PreviewVideoName { get; set; }
    public byte Limit { get; set; }
    public int FakeStudentsCount { get; set; }
    public ICollection<CourseSeason> CourseSeasons { get; set; }
}