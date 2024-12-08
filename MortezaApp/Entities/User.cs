using Microsoft.AspNetCore.Identity;

namespace OnlineCourse.Entities;

public class User : IdentityUser<int>
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Mobile { get; set; }
}