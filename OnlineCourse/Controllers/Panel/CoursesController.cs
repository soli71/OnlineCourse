using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;

public record GetAllCoursesDto(int Id, string Name, decimal Price, int DurationTime);

public record GetCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime, string SpotPlayerCourseId, string PreviewVideo, byte Limit);

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
}

[Route("api/panel/[controller]")]
[ApiController]
public class CoursesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minioService;

    public CoursesController(ApplicationDbContext context, IMinioService minioService)
    {
        _context = context;
        _minioService = minioService;
    }

    // GET: api/panel/Courses
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetAllCoursesDto>>> GetCourses()
    {
        return await _context.Courses.Select(c => new GetAllCoursesDto(
            c.Id,
            c.Name,
            c.Price,
            c.DurationTime
        )).ToListAsync();
    }

    // GET: api/panel/Courses/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GetCourseDto>> GetCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return NotFound();
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
        return new GetCourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
            image,
            course.DurationTime,
            course.SpotPlayerCourseId,
            video,
            course.Limit
        );
    }

    // PUT: api/panel/Courses/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutCourse(int id, [FromForm] CourseUpdateDto courseUpdateDto)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        course.Name = courseUpdateDto.Name;
        course.Description = courseUpdateDto.Description;
        course.Price = courseUpdateDto.Price;
        course.SpotPlayerCourseId = courseUpdateDto.SpotPlayerCourseId;
        course.DurationTime = courseUpdateDto.DurationTime;
        course.Limit = courseUpdateDto.Limit;
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
            using (var stream = new FileStream(videoName, FileMode.Create))
            {
                await courseUpdateDto.PreviewVideo.CopyToAsync(stream);
            }
            var bucketName = "course";
            await _minioService.UploadFileAsync(bucketName, videoName, tempVideoFilePath, courseUpdateDto.PreviewVideo.ContentType);
            course.PreviewVideoName = videoName;
        }
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/panel/Courses
    [HttpPost]
    public async Task<ActionResult<Course>> PostCourse([FromForm] CourseCreateDto courseCreateDto)
    {
        if (CourseExists(courseCreateDto.Name))
        {
            return BadRequest("Course already exists");
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
            Limit = courseCreateDto.Limit
        };

        if (courseCreateDto.PreviewVideo != null)
        {
            var videoName = $"{Guid.NewGuid()}_{Path.GetFileName(courseCreateDto.PreviewVideo.FileName)}";

            string tempVideoFilePath = Path.Combine(Path.GetTempPath(), videoName);

            using (var stream = new FileStream(videoName, FileMode.Create))
            {
                await courseCreateDto.Image.CopyToAsync(stream);
            }

            await _minioService.UploadFileAsync(bucketName, videoName, tempVideoFilePath, courseCreateDto.PreviewVideo.ContentType);
            course.PreviewVideoName = videoName;
        }
        // Create the course entity

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCourse), new { id = course.Id }, course);
    }

    // DELETE: api/panel/Courses/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCourse(int id)
    {
        var course = await _context.Courses.FindAsync(id);
        if (course == null)
        {
            return NotFound();
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        await _minioService.DeleteFileAsync("course", course.ImageFileName);

        return NoContent();
    }

    private bool CourseExists(string name)
    {
        return _context.Courses.Any(e => e.Name == name);
    }
}