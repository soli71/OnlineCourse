namespace OnlineCourse.Products.Entities;

public interface ICreatedAudit
{
    DateTime CreatedAt { get; set; }
    int CreatedBy { get; set; }
}