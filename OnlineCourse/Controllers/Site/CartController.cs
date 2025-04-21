using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Carts;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Products.Entities;
using OnlineCourse.Products.Services;
using OnlineCourse.Services;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

public class CreateCartDto
{
    public string CartId { get; set; }
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public CreateCartDto(int productId)
    {
        ProductId = productId;
    }
}

public interface ICartItemCount
{
    public int ProductItemCount { get; }
}

public class CourseCartItemModel
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public int ProductId { get; set; }
    public string Image { get; set; }
    public string Message { get; set; }
    public int Quantity { get; set; }
}

public class CourseCartListModel : ICartItemCount
{
    public List<CourseCartItemModel> CourseCartItems { get; set; } = new();

    public int ProductItemCount => CourseCartItems.Count;
}

public class ProductCartItemModel
{
    public string Name { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public string Image { get; set; }
    public string Message { get; set; }
    public int Quantity { get; set; }
}

public class PhysicalProductCartListModel : ICartItemCount
{
    public List<ProductCartItemModel> ProductCartItems { get; set; } = new();
    public int ProductItemCount => ProductCartItems.Count;
}

public class CartListModel
{
    public CourseCartListModel CourseCartListModel { get; set; } = new();
    public PhysicalProductCartListModel PhysicalProductCartListModel { get; set; } = new();
    public int ProductItemCount => CourseCartListModel.ProductItemCount + PhysicalProductCartListModel.ProductItemCount;

    public decimal TotalPrice { get; set; }

    public decimal Discount { get; set; }
}

[ApiController]
[Route("api/site/[controller]")]
public class CartController : BaseController
{
    private readonly IMinioService _minioService;
    private readonly ApplicationDbContext _context;
    private readonly ICourseCapacityService _courseCapacityService;

    public CartController(ApplicationDbContext context, IMinioService minioService, ICourseCapacityService courseCapacityService)
    {
        _context = context;
        _minioService = minioService;
        _courseCapacityService = courseCapacityService;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateCartDto createCartDto)
    {
        var product = _context.Products.FirstOrDefault(c => c.Id == createCartDto.ProductId);
        if (product is null)
            return NotFoundB("محصول مورد نظر یافت نشد");

        Expression<Func<Cart, bool>> func = c => c.Id == Guid.Empty;
        int userId = 0;
        if (!string.IsNullOrEmpty(createCartDto.CartId))
        {
            Guid.TryParse(createCartDto.CartId, out var cartId);
            if (cartId == Guid.Empty)
                return BadRequestB("شناسه سبد خرید نامعتبر است");
            func = x => x.Id == cartId && x.Status == CartStatus.Active;
        }
        else
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                var user = HttpContext.User;
                int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
                func = x => x.UserId == userId && x.Status == CartStatus.Active;
            }
        }

        var cart = await _context.Carts.Include(c => c.CartItems)
            .FirstOrDefaultAsync(func);

        if (cart is null)
        {
            cart = new Cart
            {
                //UserId = userId,
                Status = CartStatus.Active,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        ProductId = createCartDto.ProductId,
                        Price = product.Price,
                        Quantity=createCartDto.Quantity
                    }
                },
                UserId = userId == 0 ? null : userId
            };
            _context.Carts.Add(cart);
        }
        else
        {
            if (product is Course course)
            {
                if (!course.IsPublish)
                    return BadRequestB("دوره مورد نظر در دسترس نیست");
                else if (cart.CartItems.Any(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete))
                    return BadRequestB("این دوره در سبد خرید شما موجود می باشد.");
                else if (!await _courseCapacityService.ExistCourseCapacityAsync(course.Id))
                    return BadRequestB("ظرفیت دوره تکمیل می‌باشد");
                else

                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Quantity = createCartDto.Quantity,
                        Price = product.Price
                    });
            }
            else if (product is PhysicalProduct phys)
            {
                if (cart.CartItems.Any(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete))
                {
                    var cartProduct = cart.CartItems.FirstOrDefault(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete);
                    if (cartProduct.Quantity + createCartDto.Quantity > phys.StockQuantity)
                        return BadRequestB("موجودی محصول کافی نیست");
                    else
                        cartProduct.Quantity += createCartDto.Quantity;
                }
                else if (phys.StockQuantity <= 0)
                    return BadRequestB("موجودی محصول کافی نیست");
                else

                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Quantity = createCartDto.Quantity,
                        Price = product.Price
                    });
            }
        }

        await _context.SaveChangesAsync();
        return OkB(new { CartId = cart.Id.ToString() });
    }

    [HttpGet()]
    public async Task<IActionResult> Get(string id)
    {
        Expression<Func<Cart, bool>> func = c => c.Id == Guid.Empty;

        if (!string.IsNullOrEmpty(id))
        {
            Guid.TryParse(id, out var cartId);
            if (cartId == Guid.Empty)
                return BadRequestB("شناسه سبد خرید نامعتبر است");
            func = x => x.Id == cartId && x.Status == CartStatus.Active;
        }
        else
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                var user = HttpContext.User;
                int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
                func = x => x.UserId == userId && x.Status == CartStatus.Active;
            }
        }

        var cart = await _context.Carts
            .Include(x => x.CartItems.Where(c => !c.IsDelete))
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(func);

        if (cart == null || !cart.CartItems.Any())
            return OkB(new CartListModel());

        decimal totalPrice = 0m;
        var items = new List<CartListModel>();

        var courseList = new CourseCartListModel();
        var physicalProductList = new PhysicalProductCartListModel();

        foreach (var ci in cart.CartItems)
        {
            var prod = ci.Product;
            string message = null;

            if (prod is Course crs)
            {
                var courseItem = courseList.CourseCartItems.FirstOrDefault(c => c.ProductId == prod.Id);
                if (courseItem is not null)
                    courseItem.Quantity += ci.Quantity;
                else
                    courseList.CourseCartItems.Add(new CourseCartItemModel
                    {
                        Name = crs.Name,
                        ProductId = crs.Id,
                        Image = await _minioService.GetFileUrlAsync(MinioKey.Course, crs.DefaultImageFileName),
                        Message = message,
                        Quantity = ci.Quantity,
                        Price = ci.Price
                    });

                if (!crs.IsPublish)
                    message = "این دوره غیرفعال شده است";
                else if (!await _courseCapacityService.ExistCourseCapacityAsync(crs.Id))
                    message = "ظرفیت دوره تکمیل است";

                if (!string.IsNullOrEmpty(message))
                    ci.IsDelete = true;
            }
            else if (prod is PhysicalProduct pp)
            {
                var phyProduct = physicalProductList.ProductCartItems.FirstOrDefault(c => c.ProductId == prod.Id);
                if (phyProduct is not null)
                    phyProduct.Quantity += ci.Quantity;
                else
                    physicalProductList.ProductCartItems.Add(new ProductCartItemModel
                    {
                        Name = pp.Name,
                        ProductId = pp.Id,
                        Image = await _minioService.GetFileUrlAsync(MinioKey.PhysicalProduct, pp.DefaultImageFileName),
                        Message = message,
                        Quantity = ci.Quantity,
                        Price = ci.Price
                    });

                if (pp.StockQuantity < ci.Quantity)
                {
                    message = "موجودی محصول کافی نیست";
                    ci.IsDelete = true;
                }
            }
        }

        if (cart.CartItems.All(x => x.IsDelete))
        {
            cart.Status = CartStatus.Close;
        }

        await _context.SaveChangesAsync();

        var dto = new CartListModel
        {
            CourseCartListModel = courseList,
            PhysicalProductCartListModel = physicalProductList,
            TotalPrice = cart.CartItems.Where(x => !x.IsDelete).Sum(x => x.Price * x.Quantity),
            Discount = 0
        };
        return OkB(dto);
    }

    [HttpDelete("{id}/product/{productId}")]
    public async Task<IActionResult> Delete(string id, int productId)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        Guid.TryParse(id, out var cartId);

        Expression<Func<Cart, bool>> condition = c => c.UserId == 23332323232;
        if (cartId != Guid.Empty)
            condition = c => c.Id == cartId && c.Status == CartStatus.Active;
        else if (userId != default)
            condition = c => c.UserId == userId && c.Status == CartStatus.Active;

        var cart = await _context.Carts
            .Include(c => c.CartItems)
            .FirstOrDefaultAsync(condition);
        if (cart == null)
            return NotFoundB("سبد خرید شما خالی است");

        var item = cart.CartItems.FirstOrDefault(ci => ci.ProductId == productId && !ci.IsDelete);
        if (item == null)
            return NotFoundB("محصول مورد نظر در سبد وجود ندارد");

        item.IsDelete = true;
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpPatch("{id}/assign-to-user")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> AssignCartToUser(string id)
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(id) && c.Status == CartStatus.Active);
        if (cart == null)
            return NotFoundB("سبد خرید مورد نظر یافت نشد");
        if (cart.UserId == null)
        {
            cart.UserId = userId;
            await _context.SaveChangesAsync();
        }
        else if (cart.UserId != userId)
            return BadRequestB("سبد خرید متعلق به کاربر دیگری است");

        return OkB();
    }
}