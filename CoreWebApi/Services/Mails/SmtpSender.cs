using BeSwarm.CoreWebApi.Models;
using BeSwarm.CoreWebApi.Services.Errors;

using Confluent.Kafka;

using MailKit.Net.Smtp;
using MailKit.Security;

using MimeKit;
using MimeKit.Utils;

using Polly;
using Polly.Retry;

using System.Diagnostics;
using System.Net.Mail;


namespace BeSwarm.CoreWebApi.Services.Mails
{
    public class ResultSmtpSender
    {
        public string MimeMessage { get; set; }
        public string Satus { get; set; }
    }

    public class SmtpSender : IMailSender
    {
        ConfigMail mailConfig;
        IDispatchCriticalInternalError dispatch_error;
        AsyncRetryPolicy policy;
        public SmtpSender(ConfigMail _mailConfig, IDispatchCriticalInternalError _dispatch_error)
        {
            mailConfig = _mailConfig;
            dispatch_error = _dispatch_error;
            policy = Policy.Handle<Exception>().WaitAndRetryAsync(new[] { TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500) });
        }
        public async Task<ResultAction<ResultSmtpSender>> SendAsync(SendedMail mail)
        {
            ResultAction<ResultSmtpSender> res = new();

            try
            {
                await policy.ExecuteAsync(async () =>
                {
                    using (var client = new MailKit.Net.Smtp.SmtpClient())
                    {

                        client.Connect(mailConfig.smtpHost, mailConfig.smtpPort, SecureSocketOptions.SslOnConnect);

                        client.Authenticate(mailConfig.userName, mailConfig.password);

                        var message = new MimeMessage();
                        message.From.Add(MailboxAddress.Parse(mail.Mail.From));
                        foreach (var item in mail.Mail.To)
                        {
                            message.To.Add(MailboxAddress.Parse(item));
                        }
                        foreach (var item in mail.Mail.Cc)
                        {
                            message.Cc.Add(MailboxAddress.Parse(item));
                        }
                        var _body = new BodyBuilder();
                        message.Subject = mail.Mail.Subject;

                        if (mail.Mail.IsHtml)
                        {
                            _body.HtmlBody = mail.Mail.Body;
                        }
                        else
                        {
                            _body.TextBody = mail.Mail.Body;
                        }
                        if (!string.IsNullOrEmpty(mail.Mail.InRepyToMessageID))
                        {
                            message.InReplyTo = mail.Mail.InRepyToMessageID;
                            message.References.Add(mail.Mail.InRepyToMessageID);
                        }
                        message.MessageId = mail.MessageID;
                        foreach (var item in mail.Mail.Attachments)
                        {
                            if (string.Compare(item.Encoding, "Base64", true) == 0)
                            {
                                var bytes = Convert.FromBase64String(item.Datas);
                                _body.Attachments.Add(item.Name, bytes, MimeKit.ContentType.Parse(item.MediaType));
                            }

                        }
                        message.Body = _body.ToMessageBody();
                        res.datas.Satus = await client.SendAsync(message);
                        res.datas.MimeMessage = message.ToString();
                        client.Disconnect(true);
                    }
                });
            }
            catch (Exception ex)
            {
                await dispatch_error.DispatchCritical(ex);
                res.SetError(new(ex.Message), StatusAction.internalerror);
            }
            return res;

        }


    }
}
