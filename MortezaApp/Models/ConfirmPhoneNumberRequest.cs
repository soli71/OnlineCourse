namespace OnlineCourse.Models;

public class ConfirmPhoneNumberRequest
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
}
