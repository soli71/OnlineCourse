using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Identity.Entities;

namespace OnlineCourse.Controllers.Panel;

public class UpdateSiteUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class CreateSiteUserDto
{
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public string Password { get; set; }
}

public class SiteUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}

public class ListUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string PhoneNumber { get; set; }
}

[Route("api/panel/[controller]")]
[Authorize(Roles = "Admin")]
[ApiController]
public class AdminUsersController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;

    public AdminUsersController(ApplicationDbContext context, UserManager<User> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: api/PanelUsers
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest pagedRequest)
    {
        var query = _context.Users.Where(c => c.Type == UserType.Admin).AsQueryable();
        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            query = query.Where(c => c.FirstName.Contains(pagedRequest.Search) || c.LastName.Contains(pagedRequest.Search) || c.Email.Contains(pagedRequest.Search));
        }

        var totalCount = await query.CountAsync();
        query = query.Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);

        var result = await query.Where(c => c.Type == UserType.Admin).Select(c => new PanelUserListDto
        {
            Email = c.Email,
            FirstName = c.FirstName,
            Id = c.Id,
            LastName = c.LastName,
        }).ToListAsync();
        return OkB(new PagedResponse<List<PanelUserListDto>>
        {
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            Result = result,
            TotalCount = totalCount
        });
    }

    // GET: api/PanelUsers/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }

        return OkB(new PanelUserDto
        {
            Email = user.Email,
            FirstName = user.FirstName,
            Id = user.Id,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            IsActive = !user.Inactive
        });
    }

    // PUT: api/PanelUsers/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(int id, UpdateUserDto updateUserDto)
    {
        if (id != updateUserDto.Id)
        {
            return BadRequestB("یوزر مورد نظر یافت نشد");
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }

        user.FirstName = updateUserDto.FirstName;
        user.LastName = updateUserDto.LastName;
        user.Mobile = updateUserDto.Mobile;

        _context.Entry(user).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFoundB("یوزر مورد نظر یافت نشد");
            }
            else
            {
                throw;
            }
        }

        return OkB();
    }

    // POST: api/PanelUsers
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<IActionResult> PostUser(RegisterUserDto createUserDto)
    {
        var user = new User
        {
            UserName = createUserDto.Email,
            Email = createUserDto.Email,
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            Mobile = createUserDto.Mobile,
            PhoneNumberConfirmed = true,
            EmailConfirmed = true,
            Type = UserType.Admin
        };

        // Assuming you have a method to create a user with a password
        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return BadRequestB("");
        }

        return OkB();
    }

    [HttpPatch("{id}/deactive")]
    public async Task<IActionResult> Inactive(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(c => c.Id == id && c.Type == UserType.Admin);
        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }
        user.Inactive = true;
        await _context.SaveChangesAsync();
        return OkB();
    }

    [HttpPatch("{id}/active")]
    public async Task<IActionResult> Active(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(c => c.Id == id && c.Type == UserType.Admin);
        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }
        user.Inactive = false;
        await _context.SaveChangesAsync();
        return OkB();
    }

    //Chane current user password
    [HttpPut("ChangePassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto changePasswordDto)
    {
        var user = await _userManager.FindByIdAsync(changePasswordDto.UserId.ToString());
        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var result = await _userManager.ResetPasswordAsync(user, token, changePasswordDto.NewPassword);

        if (!result.Succeeded)
        {
            return BadRequestB("رمز عبور فعلی اشتباه است");
        }
        return OkB();
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}

public class ChangePasswordDto
{
    public int UserId { get; set; }
    public string NewPassword { get; set; }
}

public class PanelUserListDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
}

public class PanelUserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public bool IsActive { get; set; }
}