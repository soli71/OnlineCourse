namespace OnlineCourse.Products.Services
{
    public interface ICourseCapacityService
    {
        Task<bool> ExistCourseCapacityAsync(int courseId);

        Task<int> CourseStudentCountAsync(int courseId);
    }
}