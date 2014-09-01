using System.Configuration;

namespace NetMQConsole
{
    public static class ConfigHelper
    {
        public static string[] NumRange
        {
            get
            {
                return ConfigurationManager.AppSettings["NumRange"].Split('-');
            }
        }

        public static string MailServer
        {
            get
            {
                return ConfigurationManager.AppSettings["MailServer"];
            }
        }

        public static string MailUsername
        {
            get
            {
                return ConfigurationManager.AppSettings["MailUsername"];
            }
        }

        public static string MailPassword
        {
            get
            {
                return ConfigurationManager.AppSettings["MailPassword"];
            }
        }

        public static int ClearConsolePeriod
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["ClearConsolePeriodSeconds"]) * 1000;
            }
        }

        public static int GetMailsPeriod
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["GetMailsPeriodSeconds"])  * 1000;
            }
        }

        public static int UsersPerTime
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["UsersPerTime"]);
            }
        }

        public static int UsersGeneratedPerServer
        {
            get
            {
                return int.Parse(ConfigurationManager.AppSettings["UsersGeneratedPerServer"]);
            }
        }

        public static string[] MailDomains
        {
            get
            {
                return ConfigurationManager.AppSettings["MailDomains"].Split(';');
            }
        }
    }
}
