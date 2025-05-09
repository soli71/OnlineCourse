using Microsoft.EntityFrameworkCore;
using OnlineCourse.Contexts;
using OnlineCourse.Identity.Entities;
using OnlineCourse.Products.Entities;
using OnlineCourse.Services;

namespace OnlineCourse.Orders.Services
{
    public class LicenseService(ISpotPlayerService spotPlayerService, ISmsService smsService, ApplicationDbContext applicationDbContext)
    {
        public async Task GenerateLicenseAsync(Course course, User user, string spotPlayerCourseId, int orderDetailId, bool isTest)
        {
            bool.TryParse(Environment.GetEnvironmentVariable("SpotPlayerTestMode"), out bool isTestMode);
            var spotResult = spotPlayerService
                               .GetLicenseAsync(course.SpotPlayerCourseId, user.UserName, isTestMode).Result;

            if (spotResult.IsSuccess)
            {
                var licenseEntry = new License
                {
                    Key = spotResult.Result.Key,
                    UserId = user.Id,
                    OrderDetailId = orderDetailId,
                    IssuedDate = DateTime.UtcNow,
                    ExpirationDate = null,
                    Status = LicenseStatus.Active
                };
                applicationDbContext.Licenses.Add(licenseEntry);
                smsService.SendCoursePaidSuccessfully(user.PhoneNumber, course.Name).Wait();
            }
            else
            {
                throw new Exception(spotResult.Description);
            }
        }
    }
}