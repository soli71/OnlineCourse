namespace OnlineCourse.Models;

public class SendVerificationCodeResponse
{
    public int TimeToExpire { get; set; }
    public int CodeLength { get; set; }
}