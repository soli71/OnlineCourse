namespace OnlineCourse.Services
{
    public interface ISmsService
    {
        Task SendAsync(string phoneNumber, string message);
        Task SendCoursePaidSuccessfully(string phoneNumber, string courseName);
        Task SendCreateOrderMessageForAdmin(string phoneNumber, string orderCode, string courseName, string date);
        Task SendCreateOrderMessageForUser(string phoneNumber, string orderCode);
        Task SendVerificationCodeAsync(string phoneNumber, string code);
    }
}