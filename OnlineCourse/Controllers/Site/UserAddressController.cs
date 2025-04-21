using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;
using OnlineCourse.Identity.Entities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace OnlineCourse.Controllers.Site;

public static class PostalCodeValidator
{
    private static readonly Regex IranPostalCodePattern = new Regex(@"^\d{10}$", RegexOptions.Compiled);

    /// <summary>
    /// Validates if the provided string is a valid Iranian postal code (exactly 10 digits)
    /// </summary>
    /// <param name="postalCode">The postal code to validate</param>
    /// <returns>True if the postal code is valid, otherwise false</returns>
    public static bool IsValidIranianPostalCode(string postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return false;

        return IranPostalCodePattern.IsMatch(postalCode);
    }
}

public class CreateUserAddressRequestModel : IValidatableObject
{
    [Required]
    public string Address { get; set; }

    [Required]
    public string PostalCode { get; set; }

    [Required]
    public int CityId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(Address))
        {
            yield return new ValidationResult(
                "آدرس الزامی است.",
                new[] { nameof(Address) }
            );
        }
        if (string.IsNullOrEmpty(PostalCode))
        {
            yield return new ValidationResult(
                "کدپستی الزامی است.",
                new[] { nameof(PostalCode) }
            );
        }
        if (!PostalCodeValidator.IsValidIranianPostalCode(PostalCode))
        {
            yield return new ValidationResult(
                "کدپستی معتبر نیست.",
                new[] { nameof(PostalCode) }
            );
        }
        if (CityId <= 0)
        {
            yield return new ValidationResult(
                "شناسهٔ شهر باید بزرگ‌تر از ۰ باشد.",
                new[] { nameof(CityId) }
            );
        }
    }
}

[Route("api/site/[controller]")]
[ApiController]
[Authorize(Roles = "User")]
public class UserAddressController : BaseController
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UserAddressController(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    [HttpPost]
    public IActionResult Post([FromBody] CreateUserAddressRequestModel model)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!_applicationDbContext.Cities.Any(c => c.Id == model.CityId))
        {
            return NotFoundB("شهر نا معتبر می باشد");
        }
        var userAddress = new UserAddress
        {
            Address = model.Address,
            PostalCode = model.PostalCode,
            CityId = model.CityId,
            UserId = int.Parse(userId)
        };

        _applicationDbContext.UserAddresses.Add(userAddress);
        _applicationDbContext.SaveChanges();
        return OkB();
    }

    [HttpPut("{id:int}")]
    public IActionResult Put(int id, CreateUserAddressRequestModel model)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userAddress = _applicationDbContext.UserAddresses
            .FirstOrDefault(x => x.Id == id && x.UserId == int.Parse(userId));
        if (userAddress == null)
        {
            return NotFoundB("آدرس مورد نظر یافت نشد");
        }

        if (!_applicationDbContext.Cities.Any(c => c.Id == model.CityId))
        {
            return NotFoundB("شهر نا معتبر می باشد");
        }
        userAddress.Address = model.Address;
        userAddress.PostalCode = model.PostalCode;
        userAddress.CityId = model.CityId;
        _applicationDbContext.SaveChanges();
        return OkB();
    }

    [HttpGet]
    public IActionResult Get()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userAddresses = _applicationDbContext.UserAddresses
            .Where(x => x.UserId == int.Parse(userId))
            .Select(x => new
            {
                x.Id,
                x.Address,
                x.PostalCode,
                CityName = x.City.Name,
                ProvinceName = x.City.Province.Name
            })
            .ToList();
        return OkB(userAddresses);
    }

    [HttpGet("{id:int}")]
    public IActionResult Get(int id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        var address = _applicationDbContext.UserAddresses.Include(c=>c.City).FirstOrDefault(c => c.Id == id && c.UserId == int.Parse(userId));
        if(address is null)
            return NotFoundB("آدرس مورد نظر یافت نشد");

        return OkB(new { Id = address.Id, Address = address.Address, CityId = address.CityId, ProvinceId = address.City.ProvinceId, PostalCode = address.PostalCode });

    }
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var userAddress = _applicationDbContext.UserAddresses
            .FirstOrDefault(x => x.Id == id && x.UserId == int.Parse(userId));
        if (userAddress == null)
        {
            return NotFoundB("آدرس مورد نظر یافت نشد");
        }
        _applicationDbContext.UserAddresses.Remove(userAddress);
        _applicationDbContext.SaveChanges();
        return OkB();
    }
}