using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class CityController : BaseController
{
    private readonly ApplicationDbContext _context;

    public CityController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get(int provinceId)
    {
        var cities = _context.Cities.Where(c => c.ProvinceId == provinceId);
        var result = await cities.Select(c => new DropdownModel
        {
            Id = c.Id,
            Name = c.Name
        }).ToListAsync();
        return OkB(result);
    }
}