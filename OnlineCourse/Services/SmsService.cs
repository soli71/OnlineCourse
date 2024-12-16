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

        public async Task SendVerificationCodeAsync(string phoneNumber, string code)
        {
            var message = $"کد تایید شما : {code}\r\nhttps://mortezaaghaei.com";
            await SendAsync(phoneNumber, message);
        }

        public async Task SendCreateOrderMessageForUser(string phoneNumber, string orderCode)
        {
            var message = $"سفارش {orderCode} با موفقیت ثبت شد.\r\nلطفا پس از پرداخت هزینه دوره با پشتیبانی در تماس باشید.\r\nhttps://mortezaaghaei.com";
            await SendAsync(phoneNumber, message);
        }

        public async Task SendCreateOrderMessageForAdmin(string phoneNumber, string orderCode, string courseName, string date)
        {
            var message = $"سفارش با شماره {orderCode} برای دوره {courseName} ثبت شد.\r\nتاریخ : {date}\r\nhttps://mortezaaghaei.com";

            await SendAsync(phoneNumber, message);
        }

        public async Task SendCoursePaidSuccessfully(string phoneNumber, string courseName)
        {
            string message = $"دوره {courseName} با موفقیت برای شما فعال شد.\r\nبرای مشاهده دوره به پروفایل کاربری خود مراجعه نمایید.\r\nhttps://mortezaaghaei.com";
            await SendAsync(phoneNumber, message);
        }
    }
}