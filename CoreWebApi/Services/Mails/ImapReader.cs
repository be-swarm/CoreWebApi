using BeSwarm.CoreWebApi.Services.Errors;

using Confluent.Kafka;

using MailKit.Net.Imap;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;

using MimeKit;
using Polly.Retry;
using Polly;

namespace BeSwarm.CoreWebApi.Services.Mails
{
    public class ImapReader : IMailReader
    {
        ConfigMail mailConfig;
        IDispatchCriticalInternalError dispatch_error;
        AsyncRetryPolicy policy;
        public ImapReader(ConfigMail _mailConfig, IDispatchCriticalInternalError _dispatch_error)
        {
            mailConfig = _mailConfig;
            dispatch_error = _dispatch_error;
            policy = Policy.Handle<Exception>().WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500) });

        }
        public async Task<ResultAction> ReadAsync(Func<MimeMessage, Task<ResultAction>> messagehandler)
        {

            ResultAction res = new();
            using (var client = new ImapClient())
            {
                try
                {
                    await policy.ExecuteAsync(async () =>
                    {
                        client.Connect(mailConfig.imapHost, mailConfig.imapPort, SecureSocketOptions.SslOnConnect);

                        client.Authenticate(mailConfig.userName, mailConfig.password);

                        client.Inbox.Open(FolderAccess.ReadWrite);
                        var uids = client.Inbox.Search(SearchQuery.NotSeen);
                        foreach (var uid in uids)
                        {
                            var message = client.Inbox.GetMessage(uid);
                            var result = await messagehandler(message);
                            if (result.IsOk) client.Inbox.AddFlags(uid, MessageFlags.Seen, true);
                        }
                        client.Disconnect(true);
                    });
                }
                catch (Exception ex)
                {
                    await dispatch_error.DispatchCritical(ex);
                    res.SetError(new(ex.Message), StatusAction.internalerror);
                }
            }
            return res;
        }
    }
}
