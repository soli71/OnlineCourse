using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Entities;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

public class CreateCartDto
{
    public int CourseId { get; set; }
    public decimal Price { get; set; }

    public CreateCartDto(int courseId, decimal price)
    {
        CourseId = courseId;
        Price = price;
    }
}

public class CartItemDto
{
    public int CourseId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Message { get; set; }

    public CartItemDto(int courseId, string name, decimal price, string message = null)
    {
        CourseId = courseId;
        Name = name;
        Price = price;
        Message = message;
    }
}

public class CartDto
{
    public List<CartItemDto> CartItems { get; set; }

    public CartDto(List<CartItemDto> cartItems)
    {
        CartItems = cartItems;
    }
}

[ApiController]
[Route("api/site/[controller]")]
[Authorize(Roles="User")]
public class CartController : BaseController
{

    private readonly ApplicationDbContext _context;

    public CartController(ApplicationDbContext context)
    {
        _context = context;
    }
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] CreateCartDto createCartDto)
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

        var cart = await _context.Carts.Include(c=>c.CartItems).FirstOrDefaultAsync(x => x.UserId == userId && x.Status == CartStatus.Active);
        if (cart is null)
        {
            cart = new Cart
            {
                UserId = userId,
                Status = CartStatus.Active,
                CartItems = new List<CartItem>
                {
                    new CartItem
                    {
                        CourseId = createCartDto.CourseId,
                        Price = createCartDto.Price
                    }
                }
            };
            _context.Carts.Add(cart);
        }
        else
        {
            if (cart.CartItems.Any(x => x.CourseId == createCartDto.CourseId))
            {
                return BadRequestB("این دوره قبلا به سبد خرید اضافه شده است");
            }
            cart.CartItems.Add(new CartItem
            {
                CourseId = createCartDto.CourseId,
                Price = createCartDto.Price
            });
        }

        await _context.SaveChangesAsync();
        return OkB();
    }
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));

        var cart = await _context.Carts
            .Include(x => x.CartItems.Where(c=>!c.IsDelete))
            .ThenInclude(x => x.Course)
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Status == CartStatus.Active );

        if (cart is null)
        {
            return OkB(null);
        }
        foreach(var cartItem in cart.CartItems)
        {
            var course = await _context.Courses.FindAsync(cartItem.CourseId);
            if (course is null)
            {
                cartItem.Message = "این دوره غیرفعال می باشد";
                cartItem.IsDelete = true;
            }
            var totalCourseOrder = await _context.OrderDetails
                .Where(x => x.CourseId == cartItem.CourseId && (x.Order.Status == OrderStatus.Paid || (x.Order.Status == OrderStatus.Pending && x.Order.OrderDate.AddMinutes(60) > DateTime.UtcNow)))
                .CountAsync();
            if (course.Limit > 0 && totalCourseOrder > course.Limit)
            {
                cartItem.Message = "ظرفیت دوره تکمیل می باشد";
                cartItem.IsDelete = true;
            }
        }

        if(cart.CartItems.All(x => x.IsDelete))
        {
            cart.Status = CartStatus.Close; 
        }

        await _context.SaveChangesAsync();
        var cartDto = new CartDto(cart.CartItems.Select(x => new CartItemDto(x.CourseId, x.Course.Name, x.Price,x.Message)).ToList());
        return OkB(cartDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = HttpContext.User;
        var userId = int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier));
        var cart = await _context.Carts.Include(x => x.CartItems).FirstOrDefaultAsync(x => x.UserId == userId && x.Status == CartStatus.Active);
        if (cart is null)
        {
            return NotFoundB("سبد خرید شما خالی می باشد");
        }
        var cartItem = cart.CartItems.FirstOrDefault(x => x.CourseId == id);
        if (cartItem is null)
        {
            return NotFoundB("دوره مورد نظر یافت نشد");
        }
        cart.CartItems.Remove(cartItem);
        await _context.SaveChangesAsync();
        return OkB();
    }
}
