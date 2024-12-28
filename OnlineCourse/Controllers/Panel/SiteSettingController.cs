using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Panel
{
    [Route("api/panel/[controller]")]
    [ApiController]
    [Authorize(Roles = "Panel,Admin")]
    public class SiteSettingController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IMinioService _minioService;

        public SiteSettingController(ApplicationDbContext applicationDbContext, IMinioService minioService)
        {
            _applicationDbContext = applicationDbContext;
            _minioService = minioService;
        }

        [HttpPut]
        public async Task<IActionResult> PutAsync([FromForm] UpdateSiteSettingDto siteSetting)
        {
            var siteSettingEntity = _applicationDbContext.SiteSettings.Find(siteSetting.Id);
            if (siteSettingEntity == null)
            {
                return NotFound();
            }

            //store MainPageImage
            if (siteSetting.MainPageImage != null)
            {
                await _minioService.DeleteFileAsync("mainpage", siteSettingEntity.MainPageImage);
                // Use a unique file name for the new image
                var imageFileName = $"{Guid.NewGuid()}_{Path.GetFileName(siteSetting.MainPageImage.FileName)}";

                string tempFilePath = Path.Combine(Path.GetTempPath(), imageFileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await siteSetting.MainPageImage.CopyToAsync(stream);
                }

                var bucketName = "mainpage";

                await _minioService.UploadFileAsync(bucketName, imageFileName, tempFilePath, siteSetting.MainPageImage.ContentType);

                siteSettingEntity.MainPageImage = imageFileName;
            }

            siteSettingEntity.FooterContent = siteSetting.FooterContent;
            siteSettingEntity.PostalCode = siteSetting.PostalCode;
            siteSettingEntity.PhoneNumber = siteSetting.PhoneNumber;
            siteSettingEntity.Email = siteSetting.Email;
            siteSettingEntity.Address = siteSetting.Address;
            siteSettingEntity.Map = siteSetting.Map;
            siteSettingEntity.AboutUs = siteSetting.AboutUs;
            siteSettingEntity.TelegramLink = siteSetting.TelegramLink;
            siteSettingEntity.InstagramLink = siteSetting.InstagramLink;
            siteSettingEntity.MainPageContent = siteSetting.MainPageContent;
            siteSettingEntity.VisibleMainPageContent = siteSetting.VisibleMainPageContent;
            siteSettingEntity.VisibleAboutUs = siteSetting.VisibleAboutUs;
            siteSettingEntity.VisibleAddress = siteSetting.VisibleAddress;
            siteSettingEntity.VisibleMap = siteSetting.VisibleMap;
            siteSettingEntity.VisibleEmail = siteSetting.VisibleEmail;
            siteSettingEntity.VisiblePhoneNumber = siteSetting.VisiblePhoneNumber;
            siteSettingEntity.VisiblePostalCode = siteSetting.VisiblePostalCode;
            siteSettingEntity.VisibleTelegramLink = siteSetting.VisibleTelegramLink;
            siteSettingEntity.VisibleInstagramLink = siteSetting.VisibleInstagramLink;
            siteSettingEntity.VisibleFooterContent = siteSetting.VisibleFooterContent;
            siteSettingEntity.VisibleMainPageBlogs = siteSetting.VisibleMainPageBlogs;
            siteSettingEntity.VisibleMainPageImage = siteSetting.VisibleMainPageImage;
            siteSettingEntity.VisibleMainPageCourses = siteSetting.VisibleMainPageCourses;
            siteSettingEntity.Title = siteSetting.Title;
            _applicationDbContext.SaveChanges();
            return Ok();
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
}

public class UpdateSiteSettingDto
{
    public byte Id { get; set; }
    public string FooterContent { get; set; }

    public string PostalCode { get; set; }

    public string PhoneNumber { get; set; }
    public string Email { get; set; }
    public string Address { get; set; }
    public string Map { get; set; }
    public string AboutUs { get; set; }

    public string TelegramLink { get; set; }
    public string InstagramLink { get; set; }

    public string MainPageContent { get; set; }
    public IFormFile MainPageImage { get; set; }
    public bool VisibleMainPageContent { get; set; }

    public bool VisibleAboutUs { get; set; }

    public bool VisibleAddress { get; set; }

    public bool VisibleMap { get; set; }
    public bool VisibleEmail { get; set; }
    public bool VisiblePhoneNumber { get; set; }
    public bool VisiblePostalCode { get; set; }
    public bool VisibleTelegramLink { get; set; }
    public bool VisibleInstagramLink { get; set; }
    public bool VisibleFooterContent { get; set; }

    public bool VisibleMainPageImage { get; set; }
    public bool VisibleMainPageBlogs { get; set; }

    public bool VisibleMainPageCourses { get; set; }
    public string Title { get; set; }
}