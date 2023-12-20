using MediatR;

using System.ComponentModel.DataAnnotations;

namespace BeSwarm.CoreWebApi.Models
{

    public class Mail
    {
        [Required][MaxLength(255)] public string From { get; set; } = "";
        [Required] public List<string> To { get; set; } = new();
        [Required] public List<string> Cc { get; set; } = new();
        [Required][MaxLength(1024)] public string Subject { get; set; } = "";
        [Required][MaxLength(1024*1024)] public string Body { get; set; } = "";
        public bool IsHtml { get; set; } = true;
        public List<EmailAttachment> Attachments { get; set; } = new();
        public string InRepyToMessageID { get; set; } = "";

    }
    public class EmailAttachment
    {
        [Required] public string Name { get; set; }
        [Required] public string Datas { get; set; }
        [Required] public string Encoding { get; set; }
        [Required] public string MediaType { get; set; }
    }

    public class SendedMail
    {
        public Mail Mail { get; set; } = new();
        public string MessageID { get; set; } = "";
        public string Status { get; set; } = "";

    }
    public class ReceivedMail
    {
        public Mail Mail { get; set; } = new();
        public string MessageID { get; set; } = "";
  
    }



}
