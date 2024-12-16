namespace OnlineCourse.Services
{
    public interface ISpotPlayerService
    {
        Task<SpotPlayerResponse> GetLicenseAsync(string courseId, string userName, bool isTest = false);
    }
}