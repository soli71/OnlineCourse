namespace OnlineCourse.Models;

public class RegisterRequest
{
    /// <summary>
    /// The user's email address which acts as a user name.
    /// </summary>
    public string PhoneNumber { get; init; }

    /// <summary>
    /// The user's password.
    /// </summary>
    public string Password { get; init; }
}