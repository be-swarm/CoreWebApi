using MediatR;

using System.ComponentModel.DataAnnotations;

namespace BeSwarm.CoreWebApi.Models
{

    public class Reply
    {
        [MaxLength(255)] public string? InReplyTo { get; set; } 
        [MaxLength(255)] public string? Sender { get; set; } 
        public DateTime? Date{ get; set; } 
        public string? Conversation { get; set; } 
    }

    public class Mail
    {
        [Required][MaxLength(255)] public string From { get; set; } = "";
        public Reply? Reply { get; set; } 
        [Required] public List<string> To { get; set; } = new();
        [Required] public List<string> Cc { get; set; } = new();
        [Required][MaxLength(1024)] public string Subject { get; set; } = "";
        [Required][MaxLength(65535)] public string Body { get; set; } = "";
        public bool IsHtml { get; set; } = true;
        public List<EmailAttachment> Attachments { get; set; } = new();
    }
    public class EmailAttachment
    {
        [Required] public string Name { get; set; }
        [Required] public string Base64Datas { get; set; }
        [Required] public string MediaType { get; set; }

    }
}
