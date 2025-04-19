using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Products.Entities;
using OnlineCourse.Products.RequestModels.Panel;
using OnlineCourse.Products.ResponseModels.Panel;
using OnlineCourse.Services;

namespace OnlineCourse.Controllers.Panel;

[Route("api/panel/[controller]")]
[Authorize(Roles = "Panel,Admin")]
[ApiController]
public class PhysicalProductsController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IMinioService _minio;

    public PhysicalProductsController(ApplicationDbContext context, IMinioService minioService)
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
            .OrderBy(p => p.Name)
            .Select(p => new PhysicalProductListResponseModel(
                 p.Id,
                 p.Name,
                 p.Price,
                 p.StockQuantity,
                 p.IsPublish
            ))
            .ToListAsync();

        return OkB(products);
    }

    // GET: api/panel/PhysicalProducts/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");

        var imageUrls = new List<string>();
        foreach (var img in product.ImagesPath ?? Array.Empty<string>())
        {
            imageUrls.Add(await _minio.GetFileUrlAsync("physical-product", img));
        }

        var dto = new PhysicalProductDetailResponseModel(
             product.Id,
             product.Name,
             product.Description,
             product.Price,
             product.StockQuantity,
             product.IsPublish
        );

        return Ok(dto);
    }

    // POST: api/panel/PhysicalProducts
    [HttpPost]
    public async Task<IActionResult> Create([FromForm] PhysicalProductCreateRequestModel dto)
    {
        if (await _context.Products.OfType<PhysicalProduct>().AnyAsync(p => p.Name == dto.Name.Trim()))
            return BadRequestB("این محصول قبلاً تعریف شده است");

        var file = dto.DefaultImage;
        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        using (var stream = System.IO.File.Create(tempPath))
        {
            await file.CopyToAsync(stream);
        }
        await _minio.UploadFileAsync("physical-product", fileName, tempPath, file.ContentType);

        var product = new PhysicalProduct
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            DefaultImageFileName = fileName,
            StockQuantity = dto.StockQuantity,
            IsPublish = dto.IsPublish,
            Slug = dto.Name,
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return OkB();
    }

    // PUT: api/panel/PhysicalProducts/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] PhysicalProductUpdatRequestModel dto)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");

        product.Name = dto.Name;
        product.Description = dto.Description;
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.IsPublish = dto.IsPublish;

        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return OkB();
    }

    [HttpPatch("{id}/images")]
    public async Task<IActionResult> AddImage(int id, [FromForm] PhysicalProductImageRequestModel dto)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");

        var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(dto.Image.FileName)}";
        var tempPath = Path.Combine(Path.GetTempPath(), fileName);
        using (var stream = System.IO.File.Create(tempPath))
        {
            await dto.Image.CopyToAsync(stream);
        }
        await _minio.UploadFileAsync("physical-product", fileName, tempPath, dto.Image.ContentType);

        // Fix: Combine the existing ImagesPath array with the new images list
        product.ImagesPath = product.ImagesPath.Append(fileName).ToArray();

        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpDelete("{id}/image")]
    public async Task<IActionResult> DeleteImage(int id, [FromQuery] string imageName)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");
        if (product.ImagesPath == null || !product.ImagesPath.Contains(imageName))
            return NotFoundB("تصویر یافت نشد");
        await _minio.DeleteFileAsync("physical-product", imageName);
        product.ImagesPath = product.ImagesPath.Where(i => i != imageName).ToArray();
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpGet("{id}/images")]
    public async Task<IActionResult> Images(int id)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");
        if (product.ImagesPath == null)
            return NotFoundB("تصویر یافت نشد");

        var imageUrls = new List<PhysicalProductImageListResponseModel>();
        foreach (var img in product.ImagesPath)
        {
            imageUrls.Add(new PhysicalProductImageListResponseModel
            {
                ImageName = img,
                ImageUrl = await _minio.GetFileUrlAsync("physical-product", img)
            });
        }

        return OkB(imageUrls);
    }

    // DELETE: api/panel/PhysicalProducts/5
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");

        if (await _context.OrderDetails.AnyAsync(o => o.ProductId == id))
            return BadRequestB("این محصول در سفارشات کاربران موجود است و قابل حذف نمی‌باشد");

        foreach (var img in product.ImagesPath ?? Array.Empty<string>())
            await _minio.DeleteFileAsync("physical-product", img);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        return OkB();
    }
}