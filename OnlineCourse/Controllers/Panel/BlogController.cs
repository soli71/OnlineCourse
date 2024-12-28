using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel
{
    [Route("api/panel/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Panel")]
    public class BlogController : BaseController
    {
        private readonly ApplicationDbContext _context;

        private readonly IMinioService _minioService;

        public BlogController(ApplicationDbContext context, IMinioService minioService)
        {
            _context = context;
            _minioService = minioService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromForm] BlogCreateDto createDto)
        {
            var blog = new Blog
            {
                Title = createDto.Title,
                Content = createDto.Content,
                Tags = createDto.Tags,
                IsPublish = createDto.IsPublish,
                CreateDate = DateTime.UtcNow,
                Slug = createDto.Title
            };
            if (createDto.Image != null)
            {
                var fileName = $"{Guid.NewGuid()}-{blog.Title}";
                string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    createDto.Image.CopyTo(stream);
                }
                await _minioService.UploadFileAsync("ma-blog", fileName, tempFilePath, createDto.Image.ContentType);
                blog.ImageFileName = fileName;
                //remove file in temp path
                System.IO.File.Delete(tempFilePath);
            }

            _context.Blogs.Add(blog);
            _context.SaveChanges();
            return OkB();
        }

        [HttpPut("{id}")]
        public IActionResult Update([FromRoute] int id, [FromForm] BlogCreateDto updateDto)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null)
            {
                return NotFoundB("بلاگ یافت نشد");
            }
            blog.Title = updateDto.Title;
            blog.Content = updateDto.Content;
            blog.Tags = updateDto.Tags;
            blog.IsPublish = updateDto.IsPublish;
            if (updateDto.Image != null)
            {
                var fileName = $"{Guid.NewGuid()}-{blog.Title}";
                string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    updateDto.Image.CopyTo(stream);
                }
                //delete old file
                _minioService.DeleteFileAsync("ma-blog", blog.ImageFileName);
                //upload new file
                _minioService.UploadFileAsync("ma-blog", fileName, tempFilePath, updateDto.Image.ContentType);
                blog.ImageFileName = fileName;
                //remove file in temp path
                System.IO.File.Delete(tempFilePath);
            }
            _context.SaveChanges();
            return OkB();
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteBlog(int id)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null)
            {
                return NotFoundB("بلاگ یافت نشد");
            }
            _context.Blogs.Remove(blog);
            _context.SaveChanges();
            return OkB();
        }

        [HttpGet]
        public IActionResult GetBlogs()
        {
            var blogs = _context.Blogs.ToList();
            var blogsDto = blogs.Select(b => new
            {
                b.Id,
                b.Title,
                b.Content,
                b.Tags,
                b.IsPublish
            }).ToList();
            return OkB(blogsDto);
        }

        [HttpGet("{id}")]
        public IActionResult GetBlog(int id)
        {
            var blog = _context.Blogs.Find(id);
            if (blog == null)
            {
                return NotFoundB("بلاگ یافت نشد");
            }
            return OkB(new
            {
                blog.Id,
                blog.Title,
                blog.Content,
                blog.Tags,
                blog.IsPublish,
                _minioService.GetFileUrlAsync("ma-blog", blog.ImageFileName).Result
            });
        }

        [HttpGet("{blogId}/SEO")]
        public IActionResult GetSEO([FromRoute] int blogId)
        {
            var blog = _context.Blogs.Find(blogId);
            if (blog == null)
            {
                return NotFoundB("بلاگ یافت نشد");
            }
            return OkB(new SEODto
            {
                Slug = blog.Slug,
                MetaTagDescription = blog.MetaTagDescription,
                MetaTitle = blog.MetaTitle,
                MetaKeywords = blog.MetaKeywords
            });
        }

        [HttpPatch("{blogId}/SEO")]
        public IActionResult UpdateSEO([FromRoute] int blogId, [FromBody] SEODto seo)
        {
            var blog = _context.Blogs.Find(blogId);
            if (blog == null)
            {
                return NotFoundB("بلاگ یافت نشد");
            }
            blog.Slug = seo.Slug;
            blog.MetaTagDescription = seo.MetaTagDescription;
            blog.MetaTitle = seo.MetaTitle;
            blog.MetaKeywords = seo.MetaKeywords;
            _context.SaveChanges();
            return OkB();
        }
    }
}

public class BlogCreateDto
{
    [Required]
    public string Title { get; set; }

    [Required]
    public string Content { get; set; }

    public string[] Tags { get; set; }
    public IFormFile Image { get; set; }
    public bool IsPublish { get; set; }
}