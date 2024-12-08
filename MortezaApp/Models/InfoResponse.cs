namespace OnlineCourse.Models;

public class InfoResponse
{
    /// <summary>
    /// The email address associated with the authenticated user.
    /// </summary>
    public string Email { get; init; }

    public string Mobile { get; set; }
    /// <summary>
    /// Indicates whether or not the <see cref="Email"/> has been confirmed yet.
    /// </summary>
    public required bool IsEmailConfirmed { get; init; }
    public required bool IsMobileConfirmed { get; init; }

}

