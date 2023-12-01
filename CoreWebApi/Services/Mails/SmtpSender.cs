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
        public async Task<ResultAction<ResultSmtpSender>> SendAsync(Mail mail, string messageId )
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
                        message.From.Add(MailboxAddress.Parse(mail.From));
                        foreach (var item in mail.To)
                        {
                            message.To.Add(MailboxAddress.Parse(item));
                        }
                        foreach (var item in mail.Cc)
                        {
                            message.Cc.Add(MailboxAddress.Parse(item));
                        }
                        var _body = new BodyBuilder();
                        message.Subject = mail.Subject;

                        if (mail.IsHtml)
                        {
                            _body.HtmlBody = mail.Body;
                        }
                        else
                        {
                            _body.TextBody = mail.Body;
                        }
                        // conversation
                        if (mail.Reply?.Conversation is { })
                        {
                            if (!string.IsNullOrEmpty(mail.Reply.InReplyTo))
                            {
                                message.InReplyTo = mail.Reply.InReplyTo;
                                message.References.Add(mail.Reply.InReplyTo);
                            }
                            using (var text = new StringWriter())
                            {
                                text.WriteLine();
                                if (mail.IsHtml) text.WriteLine("<br>");
                                text.WriteLine(">-------- Conversation --------");
                                if (mail.IsHtml) text.WriteLine("<br>");
                                text.WriteLine($"Le {mail.Reply.Date} {mail.Reply.Sender} a écrit:");
                                if (mail.IsHtml) text.WriteLine("<br>");
                                using (var reader = new StringReader(mail.Reply.Conversation))
                                {
                                    string line;

                                    while ((line = reader.ReadLine()) != null)
                                    {
                                        text.Write("> ");
                                        text.WriteLine(line);
                                        if (mail.IsHtml) text.WriteLine("<br>");
                                    }
                                }
                                text.WriteLine();
                                if (mail.IsHtml) text.WriteLine("<br>");
                                if (mail.IsHtml) _body.HtmlBody += text.ToString();
                                else _body.TextBody += text.ToString();
                            }
                        }
                        message.MessageId = messageId;
                        foreach (var item in mail.Attachments)
                        {
                            var bytes = Convert.FromBase64String(item.Base64Datas);
                            _body.Attachments.Add(item.Name, bytes, MimeKit.ContentType.Parse(item.MediaType));
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
