using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Aquc.Netdisk.Mail
{
    public class MailService
    {
        private readonly ILogger<MailService> _logger;
        public readonly string mail;
        private readonly string key;
        private readonly SmtpClient _smtpClient;
        public MailService(ILogger<MailService> logger, string mail, string key,string host)
        {
            (_logger, this.mail, this.key) = (logger, mail, key);
            _smtpClient = new SmtpClient
            {
                Port = 25,
                Credentials = new NetworkCredential(mail, key),
                Host = host
            };
        }
            
        public async Task Send(MailMessage mailMessage)
        {
            try
            {
                await _smtpClient.SendMailAsync(mailMessage);
                _logger.LogDebug("Success send mail to {m}", string.Join(", ", mailMessage.To));
            }
            catch(SmtpException e) 
            { 
                _logger.LogError("Failed to send mail message: {name} {track}",e.Message,e.StackTrace);
            }
        }
    }
}
