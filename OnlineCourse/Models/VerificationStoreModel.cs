namespace OnlineCourse.Models;

public class VerificationStoreModel
{
    public string PhoneNumber { get; set; }
    public string VerificationCode { get; set; }
    public string Token { get; set; }
    public string Password { get; set; }
    public DateTime ExpireTime { get; internal set; }
}

