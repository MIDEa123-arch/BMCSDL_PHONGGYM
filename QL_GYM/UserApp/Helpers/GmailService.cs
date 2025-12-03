using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using UserApp.Helpers;
namespace UserApp.Helpers
{
    public class GmailService
    {
        public static bool SendOTP(string userEmail, string otpCode)
        {

            try
            {
                var fromAddress = new MailAddress("thangdien0169@gmail.com", "Gym System Security");
                var toAddress = new MailAddress(GiaiMa.GiaiMaCong(userEmail, 6));

                const string appPassword = "wfjrxxlksiwzvifm";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, appPassword)
                };

                string bodyContent = $@"
                <h3>Cảnh báo bảo mật</h3>
                <p>Tài khoản của bạn vừa đăng nhập từ một địa chỉ IP lạ.</p>
                <p>Nếu là bạn, vui lòng nhập mã OTP dưới đây để tiếp tục:</p>
                <h2 style='color:red'>{otpCode}</h2>
                <p>Mã có hiệu lực trong 3 phút.</p>";

                using (var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = "Mã xác thực đăng nhập (OTP)",
                    Body = bodyContent,
                    IsBodyHtml = true
                })
                {
                    smtp.Send(message);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}