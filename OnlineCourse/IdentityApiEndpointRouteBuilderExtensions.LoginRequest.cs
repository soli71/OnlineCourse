namespace OnlineCourse;

public class LoginRequest
{
    public required string PhoneNumber { get; init; }
    public required string Password { get; init; }

}

public class PanelLoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}
