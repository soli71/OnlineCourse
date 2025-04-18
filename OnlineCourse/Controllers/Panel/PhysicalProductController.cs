﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;
/// <summary>
/// DTO for listing physical products in admin panel
/// </summary>
public record PhysicalProductListDto(
    int Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsPublish
);

/// <summary>
/// DTO for retrieving detailed information of a physical product
/// </summary>
public record PhysicalProductDetailDto(
    int Id,
    string Name,
    string Description,
    decimal Price,
    int StockQuantity,
    bool IsPublish
);

/// <summary>
/// DTO for creating a new physical product
/// </summary>
public class PhysicalProductCreateDto
{
    [Required]
    public string Name { get; init; }

    public string Description { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    [Required]
    public List<IFormFile> Images { get; init; } = new();

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }

    public bool IsPublish { get; init; }
}

public class PhysicalProductImageDto
{
    public IFormFile[] Images { get; init; }
}

public class PhysicalProductImageListDto
{
    public string ImageName { get; init; }
    public string ImageUrl { get; init; }
}

/// <summary>
/// DTO for updating an existing physical product
/// </summary>
public class PhysicalProductUpdateDto
{
    [Required]
    public string Name { get; init; }

    public string Description { get; init; }

    [Range(0, double.MaxValue)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; init; }

    public bool IsPublish { get; init; }
}

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
            .Select(p => new PhysicalProductListDto(
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

        var dto = new PhysicalProductDetailDto(
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
    public async Task<IActionResult> Create([FromForm] PhysicalProductCreateDto dto)
    {
        if (await _context.Products.OfType<PhysicalProduct>().AnyAsync(p => p.Name == dto.Name.Trim()))
            return BadRequestB("این محصول قبلاً تعریف شده است");

        var images = new List<string>();
        foreach (var file in dto.Images)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }
            await _minio.UploadFileAsync("physical-product", fileName, tempPath, file.ContentType);
            images.Add(fileName);
        }

        var product = new PhysicalProduct
        {
            Name = dto.Name.Trim(),
            Description = dto.Description,
            Price = dto.Price,
            ImagesPath = images.ToArray(),
            StockQuantity = dto.StockQuantity,
            IsPublish = dto.IsPublish
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return OkB();
    }

    // PUT: api/panel/PhysicalProducts/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromForm] PhysicalProductUpdateDto dto)
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

    [HttpPut("{id}/images")]
    public async Task<IActionResult> UpdateImages(int id, [FromForm] PhysicalProductImageDto dto)
    {
        var product = await _context.Products
            .OfType<PhysicalProduct>()
            .FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return NotFoundB("محصول فیزیکی یافت نشد");

        var images = new List<string>();
        foreach (var file in dto.Images)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            using (var stream = System.IO.File.Create(tempPath))
            {
                await file.CopyToAsync(stream);
            }
            await _minio.UploadFileAsync("physical-product", fileName, tempPath, file.ContentType);
            images.Add(fileName);
        }

        // Fix: Combine the existing ImagesPath array with the new images list
        product.ImagesPath = product.ImagesPath?.Concat(images).ToArray() ?? images.ToArray();

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

        var imageUrls = new List<PhysicalProductImageListDto>();
        foreach (var img in product.ImagesPath)
        {
            imageUrls.Add(new PhysicalProductImageListDto
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