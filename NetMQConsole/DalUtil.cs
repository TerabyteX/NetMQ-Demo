using System.Data;
using System.Data.Common;

namespace NetMQConsole
{
    public static class DalUtil
    {
        public static DataTable ExecuteSelectCommand(DbCommand command)
        {
            command.Connection.Open();
            using (var reader = command.ExecuteReader())
            {
                var table = new DataTable();
                table.Load(reader);
                command.Connection.Close();

                return table;
            }
        }

        public static int ExecuteNonQuery(DbCommand command)
        {
            var affectedRows = -1;
            try
            {
                command.Connection.Open();
                affectedRows = command.ExecuteNonQuery();
            }
            finally
            {
                command.Connection.Close();
            }

            return affectedRows;
        }

        public static string ExecuteScalar(DbCommand command)
        {
            string value = null;
            try
            {
                command.Connection.Open();
                value = command.ExecuteScalar().ToString();
            }
            finally
            {
                command.Connection.Close();
            }

            return value;
        }

        public static DbCommand CreateCommand(string connectionString, string dataProviderName)
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory(dataProviderName);
            DbConnection conn = factory.CreateConnection();
            conn.ConnectionString = connectionString;
            DbCommand comm = conn.CreateCommand();
            comm.CommandType = CommandType.StoredProcedure;

            return comm;
        }
    }
}