namespace OnlineCourse.Products.ResponseModels.Site;

public record GetSiteCourseResponseModel(int Id, string Name, string Description, decimal Price, string Image, int DurationTime, string video, int StudentsCount, bool ExistCapacity, string MetaTitle, string MetaDescription, string[] MetaKeywords);
