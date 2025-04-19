using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Identity.Entities;

namespace OnlineCourse.Controllers.Panel;

[Route("api/panel/[controller]")]
[Authorize(Roles = "Admin,Panel")]
[ApiController]
public class SiteUserController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;

    public SiteUserController(ApplicationDbContext context, UserManager<User> userManager, RoleManager<Role> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // GET: api/PanelUsers
    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest pagedRequest)
    {
        var users = _context.Users.Where(c => c.Type == UserType.Site).AsQueryable();
        if (!string.IsNullOrEmpty(pagedRequest.Search))
        {
            users = users.Where(c => c.FirstName.Contains(pagedRequest.Search) || c.LastName.Contains(pagedRequest.Search) || c.PhoneNumber.Contains(pagedRequest.Search));
        }

        var totalCount = await users.CountAsync();
        users = users.Skip((pagedRequest.PageNumber - 1) * pagedRequest.PageSize).Take(pagedRequest.PageSize);

        var result = await users.Select(c => new ListUserDto
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            PhoneNumber = c.PhoneNumber
        }).ToListAsync();
        var pagedResponse = new PagedResponse<List<ListUserDto>>
        {
            Result = result,
            PageNumber = pagedRequest.PageNumber,
            PageSize = pagedRequest.PageSize,
            TotalCount = totalCount
        };
        return OkB(pagedResponse);
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
        return OkB(new SiteUserDto
        {
            PhoneNumber = user.PhoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Id = user.Id,
            IsActive = !user.Inactive
        });
    }

    // PUT: api/PanelUsers/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(int id, UpdateSiteUserDto updateUserDto)
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
    public async Task<IActionResult> PostUser(CreateSiteUserDto createUserDto)
    {
        if (await _userManager.Users.AnyAsync(c => c.PhoneNumber == createUserDto.PhoneNumber))
        {
            return BadRequestB("این شماره تلفن قبلا ثبت شده است");
        }
        var user = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            PhoneNumber = createUserDto.PhoneNumber,
            UserName = createUserDto.PhoneNumber,
            Email = createUserDto.Email,
            Type = UserType.Site,
            PhoneNumberConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, createUserDto.Password);
        if (!result.Succeeded)
        {
            return BadRequestB("");
        }
        var role = await _context.Roles.FirstOrDefaultAsync(c => c.Name == "Site");
        if (role == null)
        {
            role = new Role
            {
                Name = "Site"
            };
            await _roleManager.CreateAsync(role);
        }
        await _userManager.AddToRoleAsync(user, role.Name);
        return OkB();
    }

    [HttpPatch("{id}/deactive")]
    public async Task<IActionResult> Inactive(int id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(c => c.Id == id && c.Type == UserType.Site);
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
        var user = await _context.Users.FirstOrDefaultAsync(c => c.Id == id && c.Type == UserType.Site);
        if (user == null)
        {
            return NotFoundB("یوزر مورد نظر یافت نشد");
        }
        user.Inactive = false;
        await _context.SaveChangesAsync();
        return OkB();
    }

    private bool UserExists(int id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}