using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Panel;

public record GetAllCoursesDto(int Id, string Name, decimal Price, int DurationTime);

public record GetCourseDto(int Id, string Name, string Description, decimal Price, string Image, int DurationTime,string SpotPlayerCourseId);

public record CourseUpdateDto
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public IFormFile Image { get; init; }
    public int DurationTime { get; init; }
    public string SpotPlayerCourseId { get; set; }

}

public record CourseCreateDto
{
    public string Name { get; init; }
    public string Description { get; init; }
    public decimal Price { get; init; }
    public IFormFile Image { get; init; }
    public int DurationTime { get; set; }
    public string SpotPlayerCourseId { get; set; }
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
        var image=await _minioService.GetFileUrlAsync("course", course.ImageFileName);

        return new GetCourseDto(
            course.Id,
            course.Name,
            course.Description,
            course.Price,
            image,
            course.DurationTime,
            course.SpotPlayerCourseId
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

            await _minioService.UploadFileAsync(bucketName, imageFileName, tempFilePath);


            course.ImageFileName = imageFileName;
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

        await _minioService.UploadFileAsync(bucketName, imageFileName, tempFilePath);


        // Create the course entity
        var course = new Course
        {
            Name = courseCreateDto.Name,
            Description = courseCreateDto.Description,
            Price = courseCreateDto.Price,
            ImageFileName = imageFileName,
            DurationTime=courseCreateDto.DurationTime,
            SpotPlayerCourseId=courseCreateDto.SpotPlayerCourseId
        };
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
