namespace OnlineCourse.Models;

public class LoginRequest
{
    public string PhoneNumber { get; init; }
    public string Password { get; init; }
}

public class Enable2FARequest
{
    public string VerificationCode { get; set; }
}

public class AuthenticatorSetupModel
{
    public string SharedKey { get; set; }
    public string QrCode { get; set; }
}

public class TwoFaInfo
{
    public bool Enable { get; set; }
}

public class Login2fa
{
    public string VerificationCode { get; set; }
}