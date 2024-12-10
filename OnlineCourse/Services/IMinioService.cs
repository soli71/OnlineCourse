namespace OnlineCourse.Services
{
    public interface IMinioService
    {
        Task DeleteFileAsync(string bucketName, string objectName);
        Task<string> GetFileUrlAsync(string bucketName, string objectName);
        Task UploadFileAsync(string bucketName, string objectName, string filePath);
    }
}