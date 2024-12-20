using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;

namespace OnlineCourse.Controllers.Site;

[Route("api/[controller]")]
[ApiController]
public class SiteSettingController : BaseController
{
    private readonly ApplicationDbContext _applicationDbContext;

    public SiteSettingController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var setting = _applicationDbContext.SiteSettings.FirstOrDefault();
        return OkB(setting);
    }
}