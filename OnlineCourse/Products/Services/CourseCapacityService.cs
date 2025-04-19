using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OnlineCourse.Contexts;
using OnlineCourse.Orders;
using OnlineCourse.Products.Entities;

namespace OnlineCourse.Products.Services;

public class CourseCapacityService : ICourseCapacityService
{
    private readonly ApplicationDbContext _context;

    public CourseCapacityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistCourseCapacityAsync(int courseId)
    {
        var course = await _context.Products.OfType<Course>().FirstOrDefaultAsync(c => c.Id == courseId);
        if (course is null)
        {
            return false;
        }

        if (course.Limit == 0)
        {
            return true;
        }

        var totalCourseOrder = await _context.OrderDetails
            .Where(x => x.ProductId == courseId &&
                   (x.Order.Status == OrderStatus.Paid ||
                   x.Order.Status == OrderStatus.Pending
                    //&&
                    // x.Order.OrderDate.AddMinutes(60) > DateTime.UtcNow
                    )
                    )
            .CountAsync();

        return totalCourseOrder < course.Limit;
    }

    public async Task<int> CourseStudentCountAsync(int courseId)
    {
        var course = await _context.Products.OfType<Course>().FirstOrDefaultAsync(c => c.Id == courseId);

        if (course is null)
        {
            return 0;
        }

        if (course.FakeStudentsCount > 0)
        {
            return course.FakeStudentsCount;
        }

        var totalCourseOrder = await _context.OrderDetails
            .Where(x => x.ProductId == courseId &&
                   x.Order.Status == OrderStatus.Paid)
            .CountAsync();

        return totalCourseOrder;
    }
}