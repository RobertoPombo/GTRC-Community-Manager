using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

namespace GTRCLeagueManager
{
    public class SQL
    {
        public static string defaultConnectionString = "Data Source=\\;Initial Catalog=;Integrated Security=True";
        public static string connectionString = defaultConnectionString;
        private static SqlCommand SqlCmd;
        private static string SqlQry;
        private static Dictionary<Type, SqlDbType> SQLTypes = new Dictionary<Type, SqlDbType> {
            { typeof(string), SqlDbType.NVarChar },
            { typeof(Int16), SqlDbType.SmallInt },
            { typeof(Int32), SqlDbType.Int },
            { typeof(long), SqlDbType.BigInt },
            { typeof(double), SqlDbType.Float },
            { typeof(float), SqlDbType.Float },
            { typeof(bool), SqlDbType.Bit },
            { typeof(DateTime), SqlDbType.DateTime } };

        public static string ConVal(DBConnection dBCon)
        {
            if (dBCon == null) { connectionString = defaultConnectionString; }
            else if (dBCon.Type == SettingsVM.Instance.DBConnectionTypes[0])
            {
                connectionString = "Data Source=" + dBCon.PCName + "\\" + dBCon.SourceName + ";Initial Catalog=" + dBCon.CatalogName + ";Integrated Security=True";
            }
            else if (dBCon.Type == SettingsVM.Instance.DBConnectionTypes[1])
            {
                connectionString = "Data Source=" + dBCon.IP6Address + "\\" + dBCon.SourceName + "," + dBCon.Port.ToString() + ";Network Library=DBMSSOCN;Initial Catalog=" + dBCon.CatalogName + ";User ID=" + dBCon.UserID + ";Password=" + dBCon.Password + ";";
            }
            return connectionString;
        }

        public static SqlDbType GetSqlDbType(Type type)
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.String: return SqlDbType.NVarChar;
                case TypeCode.Int32: return SqlDbType.Int;
                case TypeCode.Int64: return SqlDbType.BigInt;
                case TypeCode.Boolean: return SqlDbType.Bit;
                case TypeCode.DateTime: return SqlDbType.DateTime;
                default: return SqlDbType.NVarChar;
            }
        }

        public static List<dynamic> LoadSQL(string table)
        {
            string SqlQry = "SELECT * FROM " + table + ";";
            string connectionString = SQL.connectionString;
            try { using (SqlConnection connection = new SqlConnection(@connectionString)) { return connection.Query<dynamic>(SqlQry).ToList(); } }
            catch { MainVM.List[0].LogCurrentText = "Connection to database failed!"; return new List<dynamic>(); }
        }

        public static void UpdateSQL(string table, dynamic obj)
        {
            Dictionary<PropertyInfo, dynamic> dict = obj.ReturnAsDict(false, true, false, true);
            SqlQry = "";
            SqlQry += "UPDATE " + table + " SET ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += key.Name + " = @" + key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + " WHERE ID = @ID;";
            using (SqlConnection connection = new SqlConnection(ConVal(SettingsVM.Instance.ActiveDBConnection)))
            {
                try
                {
                    SqlCmd = new SqlCommand(SqlQry, connection);
                    foreach (PropertyInfo key in dict.Keys)
                    {
                        SqlCmd.Parameters.Add("@" + key.Name, GetSqlDbType(dict[key].GetType()));
                        SqlCmd.Parameters["@" + key.Name].Value = dict[key];
                    }
                    SqlCmd.Parameters.Add("@ID", SqlDbType.Int);
                    SqlCmd.Parameters["@ID"].Value = obj.ID;
                    connection.Open();
                    SqlCmd.ExecuteNonQuery();
                }
                catch { MainVM.List[0].LogCurrentText = "Connection to database failed!"; }
                finally { connection.Close(); }
            }
        }

        public static void DelSQL(string table, dynamic obj)
        {
            SqlQry = "DELETE " + table + " WHERE ID = @ID;";
            using (SqlConnection connection = new SqlConnection(ConVal(SettingsVM.Instance.ActiveDBConnection)))
            {
                try
                {
                    SqlCmd = new SqlCommand(SqlQry, connection);
                    SqlCmd.Parameters.Add("@ID" , SqlDbType.Int);
                    SqlCmd.Parameters["@ID"].Value = obj.ID;
                    connection.Open();
                    SqlCmd.ExecuteNonQuery();
                }
                catch { MainVM.List[0].LogCurrentText = "Connection to database failed!"; }
                finally { connection.Close(); }
            }
        }

        public static void AddSQL(string table, dynamic obj)
        {
            Dictionary<PropertyInfo, dynamic> dict = obj.ReturnAsDict(false, true, false, true);
            SqlQry = "";
            SqlQry += "INSERT INTO " + table + " ( ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + ") VALUES ( ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += "@" + key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + ")";
            using (SqlConnection connection = new SqlConnection(ConVal(SettingsVM.Instance.ActiveDBConnection)))
            {
                try
                {
                    SqlCmd = new SqlCommand(SqlQry, connection);
                    foreach (PropertyInfo key in dict.Keys)
                    {
                        SqlCmd.Parameters.Add("@" + key.Name, GetSqlDbType(dict[key].GetType()));
                        SqlCmd.Parameters["@" + key.Name].Value = dict[key];
                    }
                    connection.Open();
                    SqlCmd.ExecuteNonQuery();
                }
                catch { MainVM.List[0].LogCurrentText = "Connection to database failed!"; }
                finally { connection.Close(); }
            }
        }

        public static void ReseedSQL(string table, int value)
        {
            SqlQry = "DBCC CHECKIDENT(" + table + ", RESEED, " + value.ToString() + ");";
            using (SqlConnection connection = new SqlConnection(ConVal(SettingsVM.Instance.ActiveDBConnection)))
            {
                try
                {
                    SqlCmd = new SqlCommand(SqlQry, connection);
                    connection.Open();
                    SqlCmd.ExecuteNonQuery();
                }
                catch { MainVM.List[0].LogCurrentText = "Connection to database failed!"; }
                finally { connection.Close(); }
            }
        }
    }
}
