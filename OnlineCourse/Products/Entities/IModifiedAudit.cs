namespace OnlineCourse.Products.Entities;

public interface IModifiedAudit
{
    DateTime ModifiedAt { get; set; }
    int ModifiedBy { get; set; }
}