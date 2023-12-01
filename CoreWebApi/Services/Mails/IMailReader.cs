using MimeKit;

namespace BeSwarm.CoreWebApi.Services.Mails
{
    public interface IMailReader
    {
        public Task<ResultAction> ReadAsync(Func<MimeMessage, Task<ResultAction>> massagehandler);
    }
}
