using System.ComponentModel.DataAnnotations;

namespace OnlineCourse.Controllers.Panel;

public class UserDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Mobile { get; set; }
    public string Password { get; set; }
}

public class RegisterUserDto : UserDto
{
    [Required]
    public string Email { get; set; }
    [Required]
    public string Password { get; set; }
}

public class UpdateUserDto : UserDto
{
    public int Id { get; set; }
}
