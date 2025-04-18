﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Extensions;
using OnlineCourse.Orders;
using OnlineCourse.Products.Entities;
using OnlineCourse.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Panel;

public class PagedRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string Search { get; set; }
}

public class PagedResponse<T> where T : new()
{
    public T Result { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class GetAllOrdersDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; }
    public string UserPhoneNumber { get; init; }
    public string Status { get; init; }
    public decimal TotalPrice { get; init; }
    public string OrderDate { get; init; }
}

public class GetOrderDto
{
    public int Id { get; init; }
    public string OrderCode { get; init; }
    public string UserPhoneNumber { get; init; }
    public string Status { get; init; }
    public decimal TotalPrice { get; init; }
    public string OrderDate { get; init; }
    public List<GetOrderDetailsDto> OrderDetails { get; init; }
}

public class GetOrderDetailsDto
{
    public int Id { get; init; }
    public string ProductName { get; init; }
    public string ProductType { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public string License { get; init; }        // for Course
    public string Key { get; init; }            // spot-player license ID
    public string ShippingStatus { get; init; } // for PhysicalProduct
}

public class ChangeStatusDto
{
    [Required]
    public OrderStatus Status { get; init; }

    [Required]
    public string Description { get; init; }
}

[Route("api/panel/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Panel")]
public class OrderController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ISpotPlayerService _spotPlayerService;
    private readonly ISmsService _smsService;

    public OrderController(
        ApplicationDbContext context,
        ISpotPlayerService spotPlayerService,
        ISmsService smsService)
    {
        _context = context;
        _spotPlayerService = spotPlayerService;
        _smsService = smsService;
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] PagedRequest pagedRequest)
    {
        var query = _context.Orders.AsQueryable();

        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            query = query.Where(o => o.User.PhoneNumber.Contains(pagedRequest.Search)
                                    || o.OrderCode.Contains(pagedRequest.Search));
        }

        var totalCount = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize)
            .Take(pagedRequest.PageSize)
            .Select(o => new GetAllOrdersDto
            {
                Id = o.Id,
                UserPhoneNumber = o.User.PhoneNumber,
                Status = o.Status.GetDisplayValue(),
                OrderCode = o.OrderCode,
                OrderDate = o.OrderDate.ToPersianDateTime(),
                TotalPrice = o.TotalPrice
            })
            .ToListAsync();

        var response = new PagedResponse<List<GetAllOrdersDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            TotalCount = totalCount,
            Result = orders
        };

        return OkB(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFoundB("سفارش مورد نظر یافت نشد");

        var details = order.OrderDetails.Select(od => new GetOrderDetailsDto
        {
            Id = od.Id,
            ProductName = od.Product.Name,
            ProductType = od.Product switch
            {
                Course _ => "Course",
                PhysicalProduct _ => "PhysicalProduct",
                _ => "Unknown"
            },
            Quantity = od.Quantity,
            UnitPrice = od.UnitPrice,
        }).ToList();

        var dto = new GetOrderDto
        {
            Id = order.Id,
            UserPhoneNumber = order.User.PhoneNumber,
            Status = order.Status.GetDisplayValue(),
            OrderCode = order.OrderCode,
            OrderDate = order.OrderDate.ToPersianDateTime(),
            TotalPrice = order.TotalPrice,
            OrderDetails = details
        };

        return OkB(dto);
    }

    [HttpPatch("{id}/change-status")]
    public async Task<IActionResult> ChangeStatus(int id, ChangeStatusDto changeStatusDto)
    {
        var order = await _context.Orders
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFoundB("سفارش مورد نظر یافت نشد");

        if (order.Status == OrderStatus.Paid)
            return BadRequestB("این سفارش قبلا پرداخت شده است");

        if (changeStatusDto.Status == OrderStatus.Paid)
        {
            foreach (var od in order.OrderDetails)
            {
                switch (od.Product)
                {
                    case Course course:
                        // Check capacity
                        if (course.Limit > 0)
                        {
                            var inFlight = await _context.OrderDetails
                                .Where(x => x.ProductId == od.ProductId &&
                                           (x.Order.Status == OrderStatus.Paid ||
                                            (x.Order.Status == OrderStatus.Pending &&
                                             x.Order.OrderDate.AddMinutes(60) > DateTime.UtcNow)))
                                .CountAsync();

                            if (inFlight > course.Limit)
                                return BadRequestB("ظرفیت دوره تکمیل می باشد");
                        }

                        // Issue license
                        if (string.IsNullOrEmpty(course.SpotPlayerCourseId))
                            return NotFoundB("شناسه اسپات پلیر دوره یافت نشد");

                        var user = await _context.Users.FindAsync(order.UserId);
                        var spotResult = await _spotPlayerService
                            .GetLicenseAsync(course.SpotPlayerCourseId, user.UserName, true);

                        if (spotResult.IsSuccess)
                        {
                            var licenseEntry = new License
                            {
                                Key = spotResult.Result.Key,
                                UserId = order.UserId,
                                OrderDetailId = od.Id,
                                IssuedDate = DateTime.UtcNow,
                                ExpirationDate = null,
                                Status = LicenseStatus.Active
                            };
                            _context.Licenses.Add(licenseEntry);
                        }
                        else
                        {
                            od.Description += $" {spotResult.Description}";
                        }

                        await _smsService.SendCoursePaidSuccessfully(user.PhoneNumber, course.Name);
                        break;

                    case PhysicalProduct product:
                        // Stock check and decrement
                        if (product.StockQuantity < od.Quantity)
                            return BadRequestB($"موجودی محصول {product.Name} کافی نیست");

                        product.StockQuantity -= od.Quantity;
                        //await _smsService.SendPhysicalProductShippingNotice(
                        //    order.User.PhoneNumber, product.Name, od.Quantity);
                        break;

                    default:
                        return BadRequestB("نوع محصول ناشناخته است");
                }

                await _context.SaveChangesAsync();
            }
        }

        order.Status = changeStatusDto.Status;
        _context.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            Status = changeStatusDto.Status,
            Description = changeStatusDto.Description,
            UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
            Date = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpGet("status")]
    [OutputCache(Duration = 60, Tags = new[] { CacheTag.General })]
    public IActionResult GetOrderStatus()
    {
        var statuses = Enum.GetValues<OrderStatus>()
            .Select(s => new { Value = (int)s, DisplayName = s.GetDisplayValue() });

        return OkB(statuses);
    }
}