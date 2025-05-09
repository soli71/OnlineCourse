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
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

public class CreateCartDto : IValidatableObject
{
    public string CartId { get; set; }
    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public CreateCartDto(int productId)
    {
        ProductId = productId;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Quantity <= 0)
            yield return new ValidationResult("تعداد محصول باید بزرگتر از 0 باشد", new[] { nameof(Quantity) });
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
    public int ProductCount => CourseCartItems.Sum(c => c.Quantity);

    public decimal TotalPrice => CourseCartItems.Sum(c => c.Quantity * c.Price);
}

public class ProductCartItemModel
{
    public string Name { get; set; }
    public int ProductId { get; set; }
    public decimal Price { get; set; }
    public string Image { get; set; }
    public string Message { get; set; }
    public int Quantity { get; set; }
    public int StockQuantity { get; set; }
}

public class PhysicalProductCartListModel : ICartItemCount
{
    public List<ProductCartItemModel> ProductCartItems { get; set; } = new();
    public int ProductItemCount => ProductCartItems.Count;
    public decimal TotalPrice => ProductCartItems.Sum(c => c.Quantity * c.Price);
    public int ProductCount => ProductCartItems.Sum(c => c.Quantity);
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
    private readonly PhysicalProductService _physicalProductService;

    public CartController(
        ApplicationDbContext context,
        IMinioService minioService,
        ICourseCapacityService courseCapacityService,
        PhysicalProductService physicalProductService)
    {
        _context = context;
        _minioService = minioService;
        _courseCapacityService = courseCapacityService;
        _physicalProductService = physicalProductService;
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
                var existUserThisCourse = _context.Orders.Any(c => c.UserId == userId && c.OrderDetails.Any(c => c.ProductId == createCartDto.ProductId && c.Product is Course));
                if (existUserThisCourse)
                    return BadRequestB("شما قبلا این دوره را خریداری کرده اید");

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
                var courseCartItem = cart.CartItems.FirstOrDefault(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete);

                if (!course.IsPublish)
                    return BadRequestB("دوره مورد نظر در دسترس نیست");
                else if (courseCartItem is not null)
                {
                    return BadRequestB("این دوره در سبد خرید شما موجود می باشد.");
                }
                else if (!await _courseCapacityService.ExistCourseCapacityAsync(course.Id))
                    return BadRequestB("ظرفیت دوره تکمیل می‌باشد");
                else
                {
                    cart.CartItems.Add(new CartItem
                    {
                        ProductId = product.Id,
                        Quantity = 1,
                        Price = product.Price
                    });
                }
            }
            else if (product is PhysicalProduct phys)
            {
                if (cart.CartItems.Any(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete))
                {
                    var cartProduct = cart.CartItems.FirstOrDefault(ci => ci.ProductId == createCartDto.ProductId && !ci.IsDelete);

                    if (cartProduct.Quantity + createCartDto.Quantity > _physicalProductService.GetStockQuantity(product.Id))
                        return BadRequestB("موجودی محصول کافی نیست");
                    else
                        cartProduct.Quantity += createCartDto.Quantity;
                    ;
                }
                else if (_physicalProductService.GetStockQuantity(product.Id) <= 0)
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
            .Include(x => x.CartItems.Where(c => !c.IsDelete && c.Quantity > 0))
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

                if (!crs.IsPublish)
                    message = "این دوره غیرفعال شده است";
                else if (!await _courseCapacityService.ExistCourseCapacityAsync(crs.Id))
                    message = "ظرفیت دوره تکمیل است";

                if (courseItem is not null)
                {
                    courseItem.Quantity += ci.Quantity;
                }
                else
                    courseList.CourseCartItems.Add(new CourseCartItemModel
                    {
                        Name = crs.Name,
                        ProductId = crs.Id,
                        Image = await _minioService.GetFileUrlAsync(MinioKey.Course, crs.DefaultImageFileName),
                        Message = message,
                        Quantity = ci.Quantity,
                        Price = ci.Product.Price
                    });

                if (!string.IsNullOrEmpty(message))
                    _context.CartItems.Remove(ci);
            }
            else if (prod is PhysicalProduct pp)
            {
                var stockQuantity = _physicalProductService.GetStockQuantity(pp.Id);
                if (stockQuantity < ci.Quantity)
                {
                    if (stockQuantity == 0)
                        message = "به دلیل اتمام موجودی این آیتم از سبد خرید حذف گردید";
                    else
                        message = "موجودی کالا تغییر کرده است و کمتر از مقدار درخواستی شما می باشد";

                    _context.CartItems.Remove(ci);
                }

                var productStockQuantity = _physicalProductService.GetStockQuantity(prod.Id);

                var phyProduct = physicalProductList.ProductCartItems.FirstOrDefault(c => c.ProductId == prod.Id);
                if (phyProduct is not null)
                {
                    phyProduct.Quantity += ci.Quantity;
                    phyProduct.StockQuantity = productStockQuantity;
                }
                else
                    physicalProductList.ProductCartItems.Add(new ProductCartItemModel
                    {
                        Name = pp.Name,
                        ProductId = pp.Id,
                        Image = await _minioService.GetFileUrlAsync(MinioKey.PhysicalProduct, pp.DefaultImageFileName),
                        Message = message,
                        Quantity = ci.Quantity,
                        Price = ci.Product.Price,
                        StockQuantity = productStockQuantity
                    });
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
            TotalPrice = courseList.TotalPrice + physicalProductList.TotalPrice,
            Discount = 0
        };
        return OkB(dto);
    }

    [HttpDelete("{id}/product/{productId}/{all}")]
    public async Task<IActionResult> Delete(string id, int productId, bool all)
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

        if (all)
        {
            cart.CartItems.Remove(item);
        }
        else
        {
            item.Quantity--;
            if (item.Quantity <= 0)
                cart.CartItems.Remove(item);
        }
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpPatch("{id}/assign-to-user")]
    [Authorize(Roles = "User")]
    public async Task<IActionResult> AssignCartToUser(string id)
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        var cart = await _context.Carts.Include(c => c.CartItems)
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(id) && c.Status == CartStatus.Active);

        if (cart == null)
            return NotFoundB("سبد خرید مورد نظر یافت نشد");

        if (cart.UserId is not null && cart.UserId != default && cart.UserId != userId)
            return BadRequestB("سبد خرید متعلق به کاربر دیگری است");

        var existUserAnotherCart = _context.Carts.Include(c => c.CartItems)
            .ThenInclude(c => c.Product).FirstOrDefault(c => c.UserId == userId && c.Status == CartStatus.Active && c.Id != Guid.Parse(id));
        if (existUserAnotherCart is not null)
        {
            //Combine two carts
            foreach (var item in cart.CartItems)
            {
                var existItem = existUserAnotherCart.CartItems.FirstOrDefault(c => c.ProductId == item.ProductId && !c.IsDelete);
                if (existItem is not null)
                {
                    if (existItem.Product is not Course)
                        existItem.Quantity += item.Quantity;

                    _context.CartItems.Remove(item);
                }
                else
                {
                    existUserAnotherCart.CartItems.Add(item);
                }
            }
            _context.Carts.Remove(cart);
        }
        else if (cart.UserId == null)
        {
            cart.UserId = userId;
        }
        await _context.SaveChangesAsync();
        return OkB();
    }
}