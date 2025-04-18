namespace OnlineCourse;

public class LoginRequest
{
    public string PhoneNumber { get; init; }
    public string Password { get; init; }
}

public class PanelLoginRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
}