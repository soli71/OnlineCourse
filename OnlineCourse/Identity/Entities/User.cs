using Microsoft.AspNetCore.Identity;
using OnlineCourse.Orders;

namespace OnlineCourse.Identity.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Mobile { get; set; }
    public UserType Type { get; set; }
    public bool Inactive { get; set; }
    public ICollection<License> Licenses { get; set; }
}

public enum UserType
{
    Admin = 1,
    Site = 2
}