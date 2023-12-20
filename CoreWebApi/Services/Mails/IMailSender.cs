using BeSwarm.CoreWebApi.Models;

using MimeKit;

namespace BeSwarm.CoreWebApi.Services.Mails
{
    public interface IMailSender
    {
        public Task<ResultAction<ResultSmtpSender>> SendAsync(SendedMail mail);
    }
}
