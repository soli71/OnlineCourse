using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Products.Entities;
using System.Security.Claims;

namespace OnlineCourse.Controllers.Site;

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class KabinetCalculationController : BaseController
{
    private readonly ApplicationDbContext _applicationDbContext;

    public KabinetCalculationController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    [HttpGet]
    public IActionResult GetAction([FromQuery] CalculationRequest req)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        var buyCourse = _applicationDbContext.OrderDetails.FirstOrDefault(x => x.Order.UserId == userId && x.Product is Course);
        if (buyCourse == null)
        {
            return BadRequestB("شما مجاز به استفاده این ویزگی نیستید");
        }
        var costGround = req.GroundMeters * req.PricePerMeter * req.GroundPercent / 100m;
        var costWall = req.WallMeters * req.PricePerMeter * req.WallPercent / 100m;
        var costTall = req.TallMeters * req.PricePerMeter * req.TallPercent / 100m;
        var costOpen = req.OpenMeters * req.PricePerMeter * req.OpenPercent / 100m;
        var costBody = req.BodyMeters * (req.PricePerMeter * req.BodyPercent / 100m);

        // جمع هزینه‌ها + سفارشی
        var baseSum = costGround + costWall + costTall + costOpen + costBody + req.CustomItemsAmount;
        // هزینه‌ی حمل و نصب
        var transportFee = baseSum * req.TransportAndInstallationPercent / 100m;
        // مجموع نهایی
        var totalCost = baseSum + transportFee;

        return OkB(new CalculationResponse
        {
            CostGround = costGround,
            CostWall = costWall,
            CostTall = costTall,
            CostOpen = costOpen,
            CostBody = costBody,
            CustomItemsAmount = req.CustomItemsAmount,
            TransportAndInstallationFee = transportFee,
            TotalCost = totalCost
        });
    }
}

public record CalculationRequest
{
    public decimal GroundMeters { get; init; } // متر کابینت زمینی
    public decimal GroundPercent { get; init; } // درصد زیر ورودی (مثلاً 60)
    public decimal WallMeters { get; init; } // متر کابینت دیواری 90 سانت
    public decimal WallPercent { get; init; } // درصد زیر ورودی (مثلاً 60)
    public decimal TallMeters { get; init; } // متر کابینت قدی
    public decimal TallPercent { get; init; } // درصد زیر ورودی (مثلاً 180)
    public decimal OpenMeters { get; init; } // متر اپن یا جزیره
    public decimal OpenPercent { get; init; } // درصد زیر ورودی (مثلاً 100)
    public decimal BodyMeters { get; init; } // متر بدنه نما
    public decimal BodyPercent { get; init; } // درصد زیر ورودی (مثلاً 1)
    public decimal CustomItemsAmount { get; init; } // مبلغ موارد سفارشی (تومان)
    public decimal TransportAndInstallationPercent { get; init; } // درصد حمل و نصب (%)
    public decimal PricePerMeter { get; init; } // قیمت هر متر کابینت (تومان)
}

public record CalculationResponse
{
    public decimal CostGround { get; init; }
    public decimal CostWall { get; init; }
    public decimal CostTall { get; init; }
    public decimal CostOpen { get; init; }
    public decimal CostBody { get; init; }
    public decimal CustomItemsAmount { get; init; }
    public decimal TransportAndInstallationFee { get; init; }
    public decimal TotalCost { get; init; }
}

//{
//    [Route("api/site/[controller]")]
//    [ApiController]
//    [Authorize(Roles = "User")]
//    public class UserCourseController : BaseController
//    {
//        private readonly ApplicationDbContext _applicationDbContext;

//        public UserCourseController(ApplicationDbContext applicationDbContext)
//        {
//            _applicationDbContext = applicationDbContext;
//        }

//        [HttpGet]
//        public async Task<IActionResult> GetAllUserCourses()
//        {
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

//            var userCourses = await _applicationDbContext.OrderDetails
//              .Include(x => x.Order)
//              .Where(x => x.Order.UserId == userId && x.Order.Status == OrderStatus.Paid)
//              .Select(x => new GetAllUserSiteCoursesDto(x.Product.Id, x.Product.Name, x.Product.Price))
//              .ToListAsync();
//            return OkB(userCourses);
//        }

//        [HttpGet("license")]
//        public async Task<IActionResult> GetCourseLicense(int courseId)
//        {
//            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
//            var courseLicense = _applicationDbContext.Database.SqlQueryRaw<string>(@"
//                SELECT l.[Key]
//                FROM Licenses l
//                JOIN OrderDetails od ON l.OrderDetailId = od.Id
//                WHERE l.UserId = {0} AND od.ProductId = {1}", userId, courseId)
//                .ToList();
//            if (courseLicense.Count == 0)
//            {
//                return NotFoundB("دوره ای با این مشخصات یافت نشد");
//            }
//            else
//            {
//                return OkB(courseLicense);
//            }
//        }
//    }

//    public record GetAllUserSiteCoursesDto(int Id, string Name, decimal Price);
//}