using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Minio;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;
public record GetAllSiteCoursesDto(int Id, string Name, decimal Price, string Image, string Description, int DurationTime);

public record GetSiteCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime, string video);

[Route("api/site/[controller]")]
[ApiController]
public class SiteCourseController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;

    public SiteCourseController(ApplicationDbContext context, IMinioService minioService)
    {
        _context = context;
        _minioService = minioService;
    }

    // GET: api/PanelUsers
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetAllSiteCoursesDto>>> GetCourses()
    {
        var courses = await _context.Courses.ToListAsync();

        //map to GetAllCoursesDto
        var coursesDto = courses.Select(c => new GetAllSiteCoursesDto(
            c.Id,
            c.Name,
            c.Price,
             _minioService.GetFileUrlAsync("course", c.ImageFileName).Result,
            c.Description,
            c.DurationTime
        )).ToList();
        return coursesDto;
    }

    // GET: api/PanelUsers/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GetSiteCourseDto>> GetCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        //load image

        if (course == null)
        {
            return NotFound();
        }

        var imageUrl = await _minioService.GetFileUrlAsync("course", course.ImageFileName);

        string video = null;
        if (!string.IsNullOrEmpty(course.PreviewVideoName))
        {
            video = await _minioService.GetFileUrlAsync("course", course.PreviewVideoName);
        }

        var courseDto = new GetSiteCourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
            imageUrl,
            course.DurationTime,
            video
        );
        return courseDto;
    }

    [HttpGet]
    public async Task<IActionResult> CheckCourseCapacity(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course is null)
        {
            return false;
        }
        var totalCourseOrder = await _context.OrderDetails
              .Where(x => x.CourseId == courseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(30) < DateTime.UtcNow)))
              .CountAsync();
        return totalCourseOrder < course.Limit;
    }
}