namespace BeSwarm.CoreWebApi.Services.Mails
{
    public class ConfigMail
    {
        [Len(1, -1)] public string smtpHost { get; set; } = "";
        public int smtpPort { get; set; } = 465;
        [Len(1, -1)] public string imapHost { get; set; } = "";
        public int imapPort { get; set; } = 993;
        [Hidden][Len(1, -1)] public string userName { get; set; } = "";
        [Hidden][Len(1, -1)] public string password { get; set; } = "";
        [Len(1, -1)] public string from{ get; set; } = "";
       
    }
}
