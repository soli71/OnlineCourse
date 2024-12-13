using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Controllers.Panel;

namespace OnlineCourse.Controllers.Site
{
    [Route("api/site/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class UserCourseController : BaseController
    {
        private readonly ApplicationDbContext _applicationDbContext;

        public UserCourseController(ApplicationDbContext applicationDbContext)
        {
            _applicationDbContext = applicationDbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUserCourses()
        {
            
            var userId = int.Parse(User.FindFirst("Id").Value);

            var userCourses = await _applicationDbContext.OrderDetails
              .Include(x => x.Order)
              .Where(x => x.Order.UserId == userId && x.Order.Status == Entities.OrderStatus.Paid)
              .Select(x => new GetAllUserSiteCoursesDto(x.Course.Id, x.Course.Name, x.Course.Price, x.License, x.Key))
              .ToListAsync();
            return OkB(userCourses);
        }
    }

    public record GetAllUserSiteCoursesDto(int Id, string Name, decimal Price, string License, string Key);
}