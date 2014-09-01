using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;

namespace NetMQConsole
{
    public static class UserService
    {
        private static readonly ConnectionStringSettings MDServerConfig = ConfigurationManager.ConnectionStrings["MDServer"];
        public static List<string> GetEmails(int page = 1, int count = 10)
        {
            var comm = DalUtil.CreateCommand(MDServerConfig.ConnectionString, MDServerConfig.ProviderName);
            comm.CommandText = "GetAllowMailUsers";

            var param = comm.CreateParameter();
            param.ParameterName = "@Page";
            param.Value = page;
            param.DbType = DbType.Int32;
            comm.Parameters.Add(param);

            param = comm.CreateParameter();
            param.ParameterName = "@Count";
            param.Value = count;
            param.DbType = DbType.Int32;
            comm.Parameters.Add(param);

            var table = DalUtil.ExecuteSelectCommand(comm);
            var result = new List<string>();
            foreach (DataRow row in table.Rows)
            {
                result.Add(row[0].ToString());
            }

            return result;
        }

        #region 生成用户测试数据
        public static void GenerateUsers()
        {
            var upper = ConfigHelper.UsersGeneratedPerServer;
            var domains = ConfigHelper.MailDomains;
            var domainCount = domains.Length;
            var random = new Random();

            for (var i = 0; i < upper; i++)
            {
                var comm = DalUtil.CreateCommand(MDServerConfig.ConnectionString, MDServerConfig.ProviderName);
                comm.CommandText = "InsertUser";
                var param = comm.CreateParameter();
                param.ParameterName = "@Email";
                var index = random.Next(domainCount);
                param.Value = string.Format("email{0}@{1}", i.ToString(), domains[index]);
                param.DbType = DbType.String;
                comm.Parameters.Add(param);

                param = comm.CreateParameter();
                param.ParameterName = "@Name";
                param.Value = "员工" + i.ToString();
                param.DbType = DbType.String;
                comm.Parameters.Add(param);

                var num = random.Next(10);
                var mailFlag = num < 5;
                param = comm.CreateParameter();
                param.ParameterName = "@MailFlag";
                param.Value = mailFlag;
                param.DbType = DbType.Boolean;
                comm.Parameters.Add(param);
                DalUtil.ExecuteNonQuery(comm);
            }
        }
        #endregion
    }
}