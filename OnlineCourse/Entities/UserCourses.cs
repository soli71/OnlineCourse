namespace OnlineCourse.Entities;

public class UserCourses
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course Course { get; set; }
    public int UserId { get; set; }
    public User User { get; set; }
    public string License { get; set; }
    public string Key { get; set; }

}
