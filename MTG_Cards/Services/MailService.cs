using MailChimp.Net;
using MailChimp.Net.Core;
using MailChimp.Net.Interfaces;
using MailChimp.Net.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;

namespace MTG_Cards.Services
{
	public class MailService
	{
		private readonly IConfiguration _configuration;
		public MailService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendPasswordResetEmail(string username, string emailAddress, string passwordResetLink)
		{
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration.GetSection("EmailUsername").Value));
			email.To.Add(MailboxAddress.Parse(emailAddress));
			email.Subject = "Password Reset Request";

			var bodyBuilder = new BodyBuilder();

			var styles = @"
                <style>
                    body {
                        font-family: ""Inter"", sans-serif;
                        background-color: #373737;
                        color: white;
                        height: 400px;
                    }

                    h1 span {
                        color: #D9A8FF;
                    }

                    article {
                        padding: 1rem 2rem;
                        display: flex;
                        flex-direction: column;
                        height: 400px;
                    }

                    a {
                        margin-top: 2rem;
                        padding: 1rem 3rem;
                        background-color: #D9A8FF;
                        cursor: pointer;
                        text-decoration: none;
                        color: #2E2E2E;
                        width: 150px;
                        border-radius: 1rem;
                        text-align: center;
                        font-weight: bold;
                    }
                </style>
			";

			bodyBuilder.HtmlBody = @"
            <!DOCTYPE html>
            <html lang=""en"">
            <head>
                <link rel=""preconnect"" href=""https://fonts.googleapis.com"">
                <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
                <link href=""https://fonts.googleapis.com/css2?family=Inter:wght@100..900&display=swap"" rel=""stylesheet"">"
            + styles +
            $@"</head>
            <body>
                <div>
                    <h1>Magic<span>Gatherer</span></h1>
                    <article>
                        <h2>Reset Password</h2>
                        <p>Hello <b>{username}</b>, here's the password reset link you requested:</p>
                        <br /><br />
                        <a href='{passwordResetLink}'>Reset Password</a>
                    </article>
                </div>
            </body>
            </html>
			";

            email.Body = bodyBuilder.ToMessageBody();

			using var smtp = new SmtpClient();
			await smtp.ConnectAsync(_configuration.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(_configuration.GetSection("EmailUsername").Value, _configuration.GetSection("EmailPassword").Value);
			await smtp.SendAsync(email);
			smtp.Disconnect(true);
		}
	}
}
