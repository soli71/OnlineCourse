using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class ProvinceController : BaseController
{
    private readonly ApplicationDbContext _context;

    public ProvinceController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var cities = _context.Provinces
        ;
        var result = await cities.Select(c => new DropdownModel
        {
            Id = c.Id,
            Name = c.Name
        }).ToListAsync();
        return OkB(result);
    }
}