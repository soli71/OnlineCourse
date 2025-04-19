namespace OnlineCourse.Products.ResponseModels.Site;

public record GetAllSiteCoursesResponseModel(int Id, string Name, decimal Price, string Image, string Description, int DurationTime, int StudentsCount, bool ExistCapacity, string Slug);
