using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Products.Entities;
using OnlineCourse.Products.ResponseModels.Site;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
public class PhysicalProductController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minio;

    public PhysicalProductController(ApplicationDbContext context, IMinioService minioService)
    {
        _context = context;
        _minio = minioService;
    }

    // GET: api/panel/PhysicalProducts
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _context.Products
            .OfType<PhysicalProduct>()
            .Where(p => p.IsPublish)
            .OrderBy(p => p.Name)
            .Select(p => new PhysicalProductListResponseModel(
                 p.Id,
                 p.Name,
                 p.Price,
                 p.StockQuantity < 10 ? p.StockQuantity : null,
                 _minio.GetFileUrlAsync("physical-product", p.DefaultImageFileName).Result,
                 p.Slug
            ))
            .ToListAsync();
        return OkB(products);
    }

    // GET: api/panel/PhysicalProducts/5
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetById(string slug)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Slug == slug);
        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");
        var imageUrls = new List<string>();
        foreach (var img in product.ImagesPath ?? Array.Empty<string>())
        {
            imageUrls.Add(await _minio.GetFileUrlAsync("physical-product", img));
        }
        var productDto = new PhysicalProductDetailResponseModel(
            product.Id,
            product.Name,
            product.Price,
            product.StockQuantity < 10 ? product.StockQuantity : null,
            product.Description,
            _minio.GetFileUrlAsync("physical-product", product.DefaultImageFileName).Result,
            imageUrls
        );
        return OkB(productDto);
    }
}