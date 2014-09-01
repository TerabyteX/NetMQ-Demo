using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace NetMQConsole
{
    public static class MailService
    {
        static MailService()
        {
            var range = ConfigHelper.NumRange;
            RangeBelow = int.Parse(range[0]);
            RangeUpper = int.Parse(range[1]);
        }

        private static readonly SmtpClient MailClient = new SmtpClient(ConfigHelper.MailServer)
                                                                        {
                                                                            Credentials = new NetworkCredential(ConfigHelper.MailUsername,
                                                                                ConfigHelper.MailPassword)
                                                                        };
        private static readonly int RangeBelow;
        private static readonly int RangeUpper;

        public static void Send(IEnumerable<string> mails, string title, string content)
        {
            foreach (var mail in mails)
            {
                Send(mail, title, content);
            }
        }

        public static void Send(string mail, string title, string content)
        {
            var num = new Random((int)DateTime.Now.Ticks).Next(RangeBelow - 1, RangeUpper);

            string mailName = null;
            if (num <= RangeBelow)
            {
                mailName = "mail@mingdao.com";
            }
            else
            {
                mailName = string.Format("mail{0}@mingdao.com", num);
            }
            Send(mailName, mail, title, content);
        }

        public static void Send(string from, string to, string subject, string content)
        {
            var mailMessage = new MailMessage(from, to, subject, content);
            MailClient.Send(mailMessage);
        }
    }
}