using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Products.Entities;
using OnlineCourse.Products.ResponseModels.Site;
using OnlineCourse.Products.Services;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
public class CourseController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ICourseCapacityService _courseCapacityService;
    private readonly IConfiguration _configuration;

    public CourseController(ApplicationDbContext context,
                            IMinioService minioService,
                            ICourseCapacityService courseCapacityService,
                            IConfiguration configuration)
    {
        _context = context;
        _minioService = minioService;
        _courseCapacityService = courseCapacityService;
        _configuration = configuration;
    }

    // GET: api/PanelUsers
    [HttpGet]
    public async Task<IActionResult> GetCourses()
    {
        var courses = await _context.Products.OfType<Course>().Where(c => c.IsPublish).ToListAsync();

        //map to GetAllCoursesDto
        var coursesDto = courses.Select(c => new GetAllSiteCoursesResponseModel(
            c.Id,
            c.Name,
            c.Price,
             _minioService.GetFileUrlAsync(MinioKey.Course, c.DefaultImageFileName).Result,
            c.Description,
            c.DurationTime,
            c.FakeStudentsCount,
            _courseCapacityService.ExistCourseCapacityAsync(c.Id).Result,
            c.Slug
        )).ToList();

        return OkB(coursesDto);
    }

    // GET: api/PanelUsers/5
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCourse([FromRoute] string slug)
    {
        var course = await _context.Products.OfType<Course>().FirstOrDefaultAsync(c => c.IsPublish && c.Slug == slug);

        //load image

        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد.");
        }

        var imageUrl = await _minioService.GetFileUrlAsync(MinioKey.Course, course.DefaultImageFileName);

        string video = null;
        if (!string.IsNullOrEmpty(course.PreviewVideoName))
        {
            //video = await _minioService.GetFileUrlAsync(MinioKey.Course, course.PreviewVideoName);
            video = $"https://{_configuration["MinIO:Endpoint"]}/course/{course.PreviewVideoName}";
        }
        var courseCapacity = await _courseCapacityService.ExistCourseCapacityAsync(course.Id);

        var courseDto = new GetSiteCourseResponseModel(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
            imageUrl,
            course.DurationTime,
            video,
            course.FakeStudentsCount,
            courseCapacity,
            course.MetaTitle,
            course.MetaTagDescription,
            course.MetaKeywords
        );
        return OkB(courseDto);
    }

    [HttpGet("{courseId}/stream-video")]
    public async Task<IActionResult> StreamVideo(int courseId)
    {
        var course = await _context.Products.OfType<Course>().FirstOrDefaultAsync(c => c.IsPublish && c.Id == courseId);

        //load image

        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد.");
        }
        var imageUrl = await _minioService.GetFileUrlAsync(MinioKey.Course, course.DefaultImageFileName);

        return File(imageUrl, "video/mp4");
    }

    [HttpGet("{courseId}/exist-capacity")]
    public async Task<IActionResult> CheckCourseCapacity([FromRoute] int courseId)
    {
        var capacity = await _courseCapacityService.ExistCourseCapacityAsync(courseId);

        return OkB(capacity);
    }

    [HttpGet("{courseId}/seasons")]
    public async Task<IActionResult> GetCourseSeasons([FromRoute] int courseId)
    {
        var course = await _context.Products.OfType<Course>()
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