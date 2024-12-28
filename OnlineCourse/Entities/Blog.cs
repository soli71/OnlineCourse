namespace OnlineCourse.Entities;

public class Blog : SEO
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ImageFileName { get; set; }
    public string[] Tags { get; set; }
    public DateTime CreateDate { get; set; }
    public bool IsPublish { get; set; }
    public int Visit { get; set; }
}