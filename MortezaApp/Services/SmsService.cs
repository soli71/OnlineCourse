namespace OnlineCourse.Services
{
    public class SmsService : ISmsService
    {
        public async Task SendAsync(string phoneNumber, string message)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://webone-sms.ir/SMSInOutBox/SendSms?username=09221533816&password=849654&from=10002147&to={phoneNumber}&text={message}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
    }
}