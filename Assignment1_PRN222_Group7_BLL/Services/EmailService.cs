using Microsoft.Extensions.Configuration;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace Assignment1_PRN222_Group7_BLL.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAccountCredentialsAsync(string recipientEmail, string recipientName, string password, string roleName)
        {
            try
            {
                var smtpServer = _config["EmailConfig:SmtpServer"];
                var portStr = _config["EmailConfig:Port"];
                var senderEmail = _config["EmailConfig:SenderEmail"];
                var senderName = _config["EmailConfig:SenderName"] ?? "RAG AI Chatbot System";
                var senderPassword = _config["EmailConfig:SenderPassword"];
                var sslStr = _config["EmailConfig:EnableSsl"];

                // If configuration is not set or placeholder is present, log and skip sending
                if (string.IsNullOrWhiteSpace(smtpServer) || 
                    string.IsNullOrWhiteSpace(senderEmail) || 
                    string.IsNullOrWhiteSpace(senderPassword) || 
                    senderPassword == "your-app-password-here")
                {
                    Console.WriteLine($"[EmailService] Bỏ qua gửi email đến {recipientEmail} vì chưa cấu hình SMTP credentials hợp lệ.");
                    return;
                }

                int port = int.TryParse(portStr, out var p) ? p : 587;
                bool enableSsl = !bool.TryParse(sslStr, out var ssl) || ssl; // default true

                using var mail = new MailMessage();
                mail.From = new MailAddress(senderEmail, senderName);
                mail.To.Add(new MailAddress(recipientEmail, recipientName));
                mail.Subject = "Thông tin tài khoản học tập hệ thống RAG AI Chatbot";
                
                mail.Body = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                            <div style='background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); padding: 15px; text-align: center; color: white; border-top-left-radius: 8px; border-top-right-radius: 8px;'>
                                <h2 style='margin: 0;'>Chào Mừng Đến Với RAG AI Chatbot</h2>
                            </div>
                            <div style='padding: 20px;'>
                                <p>Xin chào <strong>{recipientName}</strong>,</p>
                                <p>Tài khoản học tập và nghiên cứu của bạn trên hệ thống <strong>RAG Learning Assistant</strong> đã được quản trị viên khởi tạo thành công.</p>
                                
                                <div style='background-color: #f5f5f5; padding: 15px; border-radius: 6px; margin: 20px 0;'>
                                    <table style='width: 100%; border-collapse: collapse;'>
                                        <tr>
                                            <td style='padding: 5px 0; font-weight: bold; width: 30%;'>Địa chỉ Email:</td>
                                            <td style='padding: 5px 0;'>{recipientEmail}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 5px 0; font-weight: bold;'>Mật khẩu:</td>
                                            <td style='padding: 5px 0; font-family: monospace; font-size: 1.1em;'>{password}</td>
                                        </tr>
                                        <tr>
                                            <td style='padding: 5px 0; font-weight: bold;'>Vai trò:</td>
                                            <td style='padding: 5px 0;'>{roleName}</td>
                                        </tr>
                                    </table>
                                </div>

                                <p style='text-align: center; margin: 30px 0;'>
                                    <a href='http://localhost:5054/Account/Login' style='background: linear-gradient(135deg, #2575fc 0%, #1a5cff 100%); color: white; padding: 12px 24px; text-decoration: none; border-radius: 4px; font-weight: bold; display: inline-block; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                        Đăng Nhập Ngay
                                    </a>
                                </p>

                                <p style='font-size: 0.9em; color: #666666;'><em>Lưu ý: Vì lý do bảo mật, vui lòng đổi mật khẩu ngay sau lần đăng nhập đầu tiên.</em></p>
                            </div>
                            <div style='border-top: 1px solid #e0e0e0; padding: 15px; text-align: center; font-size: 0.8em; color: #888888;'>
                                © 2026 RAG AI Chatbot System. All rights reserved.
                            </div>
                        </div>
                    </body>
                    </html>";
                
                mail.IsBodyHtml = true;

                using var smtp = new SmtpClient(smtpServer, port);
                smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);
                smtp.EnableSsl = enableSsl;
                await smtp.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                // Graceful logging of SMTP exceptions
                Console.WriteLine($"[EmailService] Lỗi khi gửi mail đến {recipientEmail}: {ex.Message}");
            }
        }
    }
}
