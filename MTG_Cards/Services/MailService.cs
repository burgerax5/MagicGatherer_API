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

		public async Task SendPasswordResetEmail(string emailAddress, string passwordResetLink)
		{
			var email = new MimeMessage();
			email.From.Add(MailboxAddress.Parse(_configuration.GetSection("EmailUsername").Value));
			email.To.Add(MailboxAddress.Parse(emailAddress));
			email.Subject = "Password Reset Request";
			email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = passwordResetLink };

			using var smtp = new SmtpClient();
			await smtp.ConnectAsync(_configuration.GetSection("EmailHost").Value, 587, SecureSocketOptions.StartTls);
			await smtp.AuthenticateAsync(_configuration.GetSection("EmailUsername").Value, _configuration.GetSection("EmailPassword").Value);
			await smtp.SendAsync(email);
			smtp.Disconnect(true);
		}
	}
}
