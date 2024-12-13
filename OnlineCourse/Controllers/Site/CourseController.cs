using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;
public class ApiResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public object Data { get; set; }
    public int StatusCode { get; set; }
    public ApiResult(bool success, string message, object data, int statusCode)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
    }


}
public record GetAllSiteCoursesDto(int Id, string Name, decimal Price, string Image, string Description, int DurationTime, int StudentsCount);

public record GetSiteCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime, string video, int StudentsCount);

[Route("api/site/[controller]")]
[ApiController]
public class CourseController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;

    public CourseController(ApplicationDbContext context, IMinioService minioService)
    {
        _context = context;
        _minioService = minioService;
    }

    // GET: api/PanelUsers
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context.Courses.ToListAsync();

        //map to GetAllCoursesDto
        var coursesDto = courses.Select(c => new GetAllSiteCoursesDto(
            c.Id,
            c.Name,
            c.Price,
             _minioService.GetFileUrlAsync("course", c.ImageFileName).Result,
            c.Description,
            c.DurationTime,
            c.FakeStudentsCount
        )).ToList();
        return OkB(coursesDto);
    }

    // GET: api/PanelUsers/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse([FromRoute] int id)
    {
        var course = await _context.Courses.FindAsync(id);

        //load image

        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد.");
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
            video,
            course.FakeStudentsCount

        );
        return OkB(courseDto);
    }

    [HttpGet("{courseId}/exist-capacity")]
    public async Task<IActionResult> CheckCourseCapacity([FromRoute] int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course is null)
        {
            return OkB(false);
        }
        var totalCourseOrder = await _context.OrderDetails
              .Where(x => x.CourseId == courseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(30) > DateTime.UtcNow)))
              .CountAsync();
        return OkB(totalCourseOrder < course.Limit && course.Limit > 0);
    }

    [HttpGet("{courseId}/seasons")]
    public async Task<IActionResult> GetCourseSeasons([FromRoute] int courseId)
    {
        var course = await _context.Courses
            .Include(c => c.CourseSeasons)
            .ThenInclude(cs => cs.HeadLines)
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }
        var seasonsDto = course.CourseSeasons.OrderBy(c => c.Order).Select(cs => new
        {
            cs.Id,
            cs.Name,
            cs.Order,
            HeadLines = cs.HeadLines.OrderBy(c => c.Order).Select(h => new
            {
                h.Id,
                h.Title,
                h.Description,
                h.Order,
                h.DurationTime
            }).ToList()
        }).ToList();

        return OkB(seasonsDto);
    }
}