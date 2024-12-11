﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Site;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;

public class BaseController : ControllerBase
{

    protected IActionResult OkB(object value)
    {
        return base.Ok(new ApiResult(true, "", value));
    }

    protected IActionResult OkB()
    {
        return base.Ok(new ApiResult(true, "", null));
    }

    protected IActionResult BadRequestB(string message)
    {
        return base.BadRequest(new ApiResult(false, message, null));
    }

    protected IActionResult NotFoundB(string message)
    {
        return base.NotFound(new ApiResult(false, message, null));
    }

}
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
[Authorize(Roles ="Admin,Panel")]
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

        query = query.Skip((pagedRequest.PageNumber-1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);

        var result = await query.Select(c => new GetAllCoursesDto(
            c.Id,
            c.Name,
            c.Price,
            c.DurationTime
        )).ToListAsync();
        return OkB(new PagedResponse<List<GetAllCoursesDto>>
        {
            PageNumber= pagedRequest.PageNumber,
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
            course.Limit
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
            Limit = courseCreateDto.Limit
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