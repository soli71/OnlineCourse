using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;

[Route("api/[controller]")]
[ApiController]
public class SiteSettingController : BaseController
{
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly IMinioService _minioService;

    public SiteSettingController(ApplicationDbContext applicationDbContext, IMinioService minioService)
    {
        _applicationDbContext = applicationDbContext;
        _minioService = minioService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync()
    {
        var siteSetting = _applicationDbContext.SiteSettings.FirstOrDefault();
        if (siteSetting.MainPageImage != null)
        {
            siteSetting.MainPageImage = await _minioService.GetFileUrlAsync("mainpage", siteSetting.MainPageImage);
        }
        return Ok(siteSetting);
    }
}