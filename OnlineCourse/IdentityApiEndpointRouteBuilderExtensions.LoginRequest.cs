namespace OnlineCourse;

public class LoginRequest
{
    public string PhoneNumber { get; init; }
    public string Password { get; init; }
}

public class LoginWithoutPasswordRequest
{
    public string PhoneNumber { get; set; }
}

public class LoginWithoutPassword
{
    public string PhoneNumber { get; set; }
    public string Code { get; set; }
}

public class VerificationResponse
{
    public int CodeLength { get; set; }
    public int TimeToExpire { get; set; }
}

public class PanelLoginRequest
{
    public string Email { get; init; }
    public string Password { get; init; }
}