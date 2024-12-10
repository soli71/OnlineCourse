using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Minio;
using OnlineCourse.Contexts;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;
public record GetAllSiteCoursesDto(int Id, string Name, decimal Price, string Image, string Description, int DurationTime);

public record GetSiteCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime);

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

        var courseDto = new GetSiteCourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
           imageUrl,
            course.DurationTime
        );
        return courseDto;
    }
}
