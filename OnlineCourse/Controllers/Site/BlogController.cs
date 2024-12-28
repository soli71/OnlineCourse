using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Extensions;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site
{
    [Route("api/site/[controller]")]
    [ApiController]
    public class BlogController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IMinioService _minioService;

        public BlogController(ApplicationDbContext context, IMinioService minioService)
        {
            _context = context;
            _minioService = minioService;
        }

        [HttpGet("top-blogs")]
        [OutputCache(Duration = 70, Tags = [CacheTag.General])]
        public async Task<IActionResult> GetTopBlogs()
        {
            var blogs = await _context.Blogs.Where(c => c.IsPublish)
                .OrderByDescending(c => c.Visit)
                .OrderByDescending(c => c.CreateDate)
                .Take(10)
                .Select(c => new
                {
                    c.Title,
                    c.Slug
                })
                .ToListAsync();
            return OkB(blogs);
        }

        [HttpGet]
        public async Task<IActionResult> GetBlogs([FromQuery] PagedRequest pagedRequest)
        {
            var query = _context.Blogs.Where(c => c.IsPublish).AsQueryable();
            if (!string.IsNullOrWhiteSpace(pagedRequest.Search))
            {
                query = query.Where(c => c.Title.Contains(pagedRequest.Search));
            }
            var blogs = query
                .OrderByDescending(c => c.CreateDate)
                .Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Tags,
                    CreateDate = c.CreateDate.ToPersianDateTime(),
                    c.Title,
                    c.Content,
                    c.Visit,
                    _minioService.GetFileUrlAsync("ma-blog", c.ImageFileName).Result,
                    c.Slug
                }).ToList();

            return OkB(new PagedResponse<object>
            {
                TotalCount = query.Count(),
                PageNumber = pagedRequest.PageNumber,
                PageSize = pagedRequest.PageSize,
                Result = blogs
            });
        }

        [HttpGet("{slug}")]
        public async Task<IActionResult> GetBlog([FromRoute] string slug)
        {
            var blog = await _context.Blogs.FirstOrDefaultAsync(c => c.Slug == slug && c.IsPublish);
            if (blog == null)
            {
                return NotFoundB("مقاله مورد نظر یافت نشد");
            }
            blog.Visit++;
            await _context.SaveChangesAsync();
            return OkB(new
            {
                blog.Id,
                blog.Tags,
                CreateDate = blog.CreateDate.ToPersianDateTime(),
                blog.Title,
                blog.Content,
                blog.Visit,
                blog.MetaTitle,
                blog.MetaTagDescription,
                blog.MetaKeywords,
                _minioService.GetFileUrlAsync("ma-blog", blog.ImageFileName).Result
            });
        }
    }
}