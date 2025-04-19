using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Products.Entities;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;

public class CourseSeasonCreateDto
{
    [Required]
    public string Name { get; set; }

    [Required]
    public int CourseId { get; set; }

    public byte Order { get; set; }
}

public class CourseSeasonUpdateDto
{
    [Required]
    public string Name { get; set; }

    public byte Order { get; set; }
}

public class GetAllCourseSeasonsDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class GetCourseSeasonDto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Course { get; set; }
    public byte Order { get; set; }
}

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]
public class SeasonController : BaseController
{
    private readonly ApplicationDbContext _context;

    public SeasonController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> PostCourseSeason([FromBody] CourseSeasonCreateDto courseSeason)
    {
        var course = new CourseSeason
        {
            Name = courseSeason.Name,
            CourseId = courseSeason.CourseId,
            Order = courseSeason.Order
        };

        _context.CourseSeasons.Add(course);
        await _context.SaveChangesAsync();

        return OkB();
    }

    [HttpGet("{seasonId}")]
    public async Task<IActionResult> GetCourseSeason([FromRoute] int seasonId)
    {
        var courseSeason = await _context.CourseSeasons.Include(c => c.Course).FirstOrDefaultAsync(c => c.Id == seasonId);
        if (courseSeason == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }

        return OkB(new GetCourseSeasonDto
        {
            Course = courseSeason.Course.Name,
            Id = courseSeason.Id,
            Name = courseSeason.Name,
            Order = courseSeason.Order
        });
    }

    [HttpDelete("{seasonId}")]
    public async Task<IActionResult> DeleteCourseSeason([FromRoute] int seasonId)
    {
        var courseSeason = await _context.CourseSeasons.FirstOrDefaultAsync(c => c.Id == seasonId);
        if (courseSeason == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }
        _context.CourseSeasons.Remove(courseSeason);
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpPut("{seasonId}")]
    public async Task<IActionResult> PutCourseSeason([FromRoute] int seasonId, [FromBody] CourseSeasonUpdateDto courseSeason)
    {
        var course = await _context.CourseSeasons.FirstOrDefaultAsync(c => c.Id == seasonId);
        if (course == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }
        course.Name = courseSeason.Name;
        course.Order = courseSeason.Order;
        await _context.SaveChangesAsync();
        return OkB();
    }

    #region TitleOfCourse

    [HttpGet("{seasonId}/headlines")]
    public async Task<IActionResult> Headlines([FromRoute] int seasonId)
    {
        var titleOfCourses = await _context.HeadLines.Where(c => c.CourseSeasonId == seasonId).OrderBy(c => c.Order)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.DurationTime,
            }).ToListAsync();
        return OkB(titleOfCourses);
    }

    [HttpPost("{seasonId}/headline")]
    public async Task<IActionResult> PostHeadline([FromRoute] int seasonId, [FromBody] SeasonHeadlineCreateDto createDto)
    {
        var season = await _context.CourseSeasons.FirstOrDefaultAsync(c => c.Id == seasonId);
        if (season == null)
        {
            return NotFoundB("این فصل یافت نشد");
        }
        var headline = new HeadLines
        {
            Title = createDto.Title,
            Description = createDto.Description,
            DurationTime = createDto.DurationTime,
            Order = createDto.Order,
            CourseSeasonId = seasonId
        };

        headline.CourseSeasonId = seasonId;
        _context.HeadLines.Add(headline);
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpPut("headline/{headlineId}")]
    public async Task<IActionResult> PutHeadline([FromRoute] int headlineId, [FromBody] SeasonHeadlineUpdateDto createDto)
    {
        var headline = await _context.HeadLines.FirstOrDefaultAsync(c => c.Id == headlineId);
        if (headline == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }
        headline.Title = createDto.Title;
        headline.Description = createDto.Description;
        headline.DurationTime = createDto.DurationTime;
        headline.Order = createDto.Order;
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpDelete("headline/{headlineId}")]
    public async Task<IActionResult> DeleteHeadline([FromRoute] int headlineId)
    {
        var headline = await _context.HeadLines.FirstOrDefaultAsync(c => c.Id == headlineId);
        if (headline == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }
        _context.HeadLines.Remove(headline);
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpGet("headline/{headlineId}")]
    public async Task<IActionResult> GetHeadline([FromRoute] int headlineId)
    {
        var headline = await _context.HeadLines.FirstOrDefaultAsync(c => c.Id == headlineId);
        if (headline == null)
        {
            return NotFoundB("این سرفصل یافت نشد");
        }
        return OkB(new
        {
            headline.Id,
            headline.Title,
            headline.Description,
            headline.DurationTime,
            headline.Order
        });
    }

    #endregion TitleOfCourse
}

public class SeasonHeadlineCreateDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public int DurationTime { get; set; }

    public byte Order { get; set; }
}

public class SeasonHeadlineUpdateDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Description { get; set; }

    [Required]
    public int DurationTime { get; set; }

    public byte Order { get; set; }
}