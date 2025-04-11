using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;
public record GetAllCoursesDto(int Id, string Name, decimal Price, int DurationTime, bool IsPublish);

public record GetCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime, string SpotPlayerCourseId, string PreviewVideo, byte Limit, bool IsPublish, int FakeStudentCount);

public record CourseUpdateDto
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public IFormFile Image { get; init; }
    public IFormFile PreviewVideo { get; init; }
    public int DurationTime { get; init; }
    public string SpotPlayerCourseId { get; init; }
    public byte Limit { get; init; }
    public int FakeStudentCount { get; init; }
    public bool IsPublish { get; init; }
}

public record CourseCreateDto
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }

    [Required]
    public IFormFile Image { get; init; }
    public IFormFile PreviewVideo { get; init; }
    public int DurationTime { get; init; }
    public string SpotPlayerCourseId { get; init; }
    public byte Limit { get; init; }
    public bool IsPublish { get; init; }
}

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]
public class CourseController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;

    public CourseController(ApplicationDbContext context, IMinioService minioService)
    {
        _context = context;
        _minioService = minioService;
    }

    // GET: api/panel/Courses
    [HttpGet]
    public async Task<IActionResult> GetCourses([FromQuery] PagedRequest pagedRequest)
    {
        var query = _context.Courses.AsQueryable();
        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            query = query.Where(c => c.Name.Contains(pagedRequest.Search));
        }
        var totalCount = await query.CountAsync();

        query = query.Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);

        var result = await query.Select(c => new GetAllCoursesDto(
            c.Id,
            c.Name,
            c.Price,
            c.DurationTime,
            c.IsPublish
        )).ToListAsync();
        return OkB(new PagedResponse<List<GetAllCoursesDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            Result = result,
            TotalCount = totalCount
        });
    }

    // GET: api/panel/Courses/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return NotFoundB("دوره مورد نظر موجود نمی باشد");
        }

        string image = null;
        string video = null;

        if (!string.IsNullOrEmpty(course.ImageFileName))
        {
            image = await _minioService.GetFileUrlAsync("course", course.ImageFileName);
        }
        if (!string.IsNullOrEmpty(image))
        {
            video = await _minioService.GetFileUrlAsync("course", course.PreviewVideoName);
        }
        return OkB(new GetCourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
            image,
            course.DurationTime,
            course.SpotPlayerCourseId,
            video,
            course.Limit,
            course.IsPublish,
            course.FakeStudentsCount
        ));
    }

    // PUT: api/panel/Courses/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCourse(int id, [FromForm] CourseUpdateDto courseUpdateDto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }

        course.Name = courseUpdateDto.Name;
        course.Description = courseUpdateDto.Description;
        course.Price = courseUpdateDto.Price;
        course.SpotPlayerCourseId = courseUpdateDto.SpotPlayerCourseId;
        course.DurationTime = courseUpdateDto.DurationTime;
        course.Limit = courseUpdateDto.Limit;
        course.FakeStudentsCount = courseUpdateDto.FakeStudentCount;
        course.IsPublish = courseUpdateDto.IsPublish;
        if (courseUpdateDto.Image != null)
        {
            await _minioService.DeleteFileAsync("course", course.ImageFileName);
            // Use a unique file name for the new image
            var imageFileName = $"{Guid.NewGuid()}_{Path.GetFileName(courseUpdateDto.Image.FileName)}";

            string tempFilePath = Path.Combine(Path.GetTempPath(), imageFileName);
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                await courseUpdateDto.Image.CopyToAsync(stream);
            }

            var bucketName = "course";

            await _minioService.UploadFileAsync(bucketName, imageFileName, tempFilePath, courseUpdateDto.Image.ContentType);

            course.ImageFileName = imageFileName;
        }

        if (courseUpdateDto.PreviewVideo != null)
        {
            await _minioService.DeleteFileAsync("course", course.PreviewVideoName);
            // Use a unique file name for the new video
            var videoName = $"{Guid.NewGuid()}_{Path.GetFileName(courseUpdateDto.PreviewVideo.FileName)}";
            string tempVideoFilePath = Path.Combine(Path.GetTempPath(), videoName);
            using (var stream = new FileStream(tempVideoFilePath, FileMode.Create))
            {
                await courseUpdateDto.PreviewVideo.CopyToAsync(stream);
            }
            var bucketName = "course";
            await _minioService.UploadFileAsync(bucketName, videoName, tempVideoFilePath, courseUpdateDto.PreviewVideo.ContentType);
            course.PreviewVideoName = videoName;
        }
        await _context.SaveChangesAsync();

        return OkB();
    }

    // POST: api/panel/Courses
    [HttpPost]
    public async Task<IActionResult> PostCourse([FromForm] CourseCreateDto courseCreateDto)
    {
        if (CourseExists(courseCreateDto.Name))
        {
            return BadRequestB("دوره مورد نظر ازقبل تعریف شده است");
        }

        var imageFileName = $"{Guid.NewGuid()}_{Path.GetFileName(courseCreateDto.Image.FileName)}";

        string tempFilePath = Path.Combine(Path.GetTempPath(), imageFileName);
        using (var stream = new FileStream(tempFilePath, FileMode.Create))
        {
            await courseCreateDto.Image.CopyToAsync(stream);
        }

        var bucketName = "course";

        await _minioService.UploadFileAsync(bucketName, imageFileName, tempFilePath, courseCreateDto.Image.ContentType);

        var course = new Course
        {
            Name = courseCreateDto.Name,
            Description = courseCreateDto.Description,
            Price = courseCreateDto.Price,
            ImageFileName = imageFileName,
            DurationTime = courseCreateDto.DurationTime,
            SpotPlayerCourseId = courseCreateDto.SpotPlayerCourseId,
            Limit = courseCreateDto.Limit,
            IsPublish = courseCreateDto.IsPublish,
            Slug = courseCreateDto.Name
        };

        if (courseCreateDto.PreviewVideo != null)
        {
            var videoName = $"{Guid.NewGuid()}_{Path.GetFileName(courseCreateDto.PreviewVideo.FileName)}";

            string tempVideoFilePath = Path.Combine(Path.GetTempPath(), videoName);

            using (var stream = new FileStream(tempVideoFilePath, FileMode.Create))
            {
                await courseCreateDto.PreviewVideo.CopyToAsync(stream);
            }

            await _minioService.UploadFileAsync(bucketName, videoName, tempVideoFilePath, courseCreateDto.PreviewVideo.ContentType);
            course.PreviewVideoName = videoName;
        }
        // Create the course entity

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return OkB();
    }

    // DELETE: api/panel/Courses/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }

        var existOrder = await _context.OrderDetails.AnyAsync(c => c.CourseId == id);
        if (existOrder)
        {
            return BadRequestB("این دوره در سفارشات کاربران موجود می باشد. لطفا دوره  را از حالت منتشر شده خارح نمایید");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        await _minioService.DeleteFileAsync("course", course.ImageFileName);

        return OkB();
    }

    [HttpGet("{id}/student-count")]
    public async Task<IActionResult> GetStudentCount(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }
        return OkB(course.FakeStudentsCount);
    }

    [HttpGet("{courseId}/seasons")]
    public async Task<IActionResult> GetCourseSeasons([FromRoute] int courseId)
    {
        var courseSeasons = await _context.CourseSeasons.Select(c => new GetAllCourseSeasonsDto
        {
            Id = c.Id,
            Name = c.Name
        }).ToListAsync();

        return OkB(courseSeasons);
    }

    [HttpGet("{courseId}/SEO")]
    public async Task<IActionResult> GetSEO(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }
        return OkB(new SEODto
        {
            Slug = course.Slug,
            MetaTitle = course.MetaTitle,
            MetaTagDescription = course.MetaTagDescription,
            MetaKeywords = course.MetaKeywords
        });
    }

    [HttpPatch("{courseId}/SEO")]
    public async Task<IActionResult> UpdateSEO(int courseId, [FromBody] SEODto seo)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }
        course.Slug = seo.Slug;
        course.MetaTitle = seo.MetaTitle;
        course.MetaTagDescription = seo.MetaTagDescription;
        course.MetaKeywords = seo.MetaKeywords;
        await _context.SaveChangesAsync();
        return OkB();
    }

    private bool CourseExists(string name)
    {
        return _context.Courses.Any(e => e.Name == name);
    }
}

public class CourseStudentCountDto
{
    public int ReserveCount { get; set; }
    public int CompletedCount { get; set; }
    public int TotalCount { get; set; }
}

public class SEODto
{
    public string Slug { get; set; }
    public string MetaTitle { get; set; }
    public string MetaTagDescription { get; set; }
    public string[] MetaKeywords { get; set; }
}