using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;

using GTRC_Community_Manager;

namespace Scripts
{
    public class SQL
    {
        public static string defaultConnectionString = "Data Source=\\;Initial Catalog=;Integrated Security=True";
        public static string ConnectionString = defaultConnectionString;
        public static SqlConnection Connection = new(ConVal(SettingsVM.Instance.ActiveDBConnection));
        private static SqlCommand SqlCmd;
        private static string SqlQry;
        private static Dictionary<Type, SqlDbType> SQLTypes = new()
        {
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
            if (dBCon == null) { ConnectionString = defaultConnectionString; }
            else if (dBCon.Type == SettingsVM.Instance.DBConnectionTypes[0])
            {
                ConnectionString = "Data Source=" + dBCon.PCName + "\\" + dBCon.SourceName + ";Initial Catalog=" + dBCon.CatalogName + ";Integrated Security=True";
            }
            else if (dBCon.Type == SettingsVM.Instance.DBConnectionTypes[1])
            {
                ConnectionString = "Data Source=" + dBCon.IP6Address + "\\" + dBCon.SourceName + "," + dBCon.Port.ToString() + ";Initial Catalog=" + dBCon.CatalogName + ";User ID=" + dBCon.UserID + ";Password=" + dBCon.Password + ";";
            }
            return ConnectionString;
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
            try { return SQL.Connection.Query<dynamic>(SqlQry).ToList(); }
            catch { MainVM.List[0].LogCurrentText = "Loading SQL table '" + table + "' failed!"; return new List<dynamic>(); }
        }

        public static void UpdateSQL(string table, dynamic obj)
        {
            Dictionary<PropertyInfo, dynamic> dict = obj.ReturnAsDict(false, false, false, true);
            SqlQry = "";
            SqlQry += "UPDATE " + table + " SET ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += key.Name + " = @" + key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + " WHERE ID = @ID;";
            try
            {
                SqlCmd = new SqlCommand(SqlQry, Connection);
                foreach (PropertyInfo key in dict.Keys)
                {
                    SqlCmd.Parameters.Add("@" + key.Name, GetSqlDbType(dict[key].GetType()));
                    SqlCmd.Parameters["@" + key.Name].Value = dict[key];
                }
                SqlCmd.Parameters.Add("@ID", SqlDbType.Int);
                SqlCmd.Parameters["@ID"].Value = obj.ID;
                SqlCmd.ExecuteNonQuery();
            }
            catch { MainVM.List[0].LogCurrentText = "Updating SQL table '" + table + "' failed!"; }
        }

        public static void DelSQL(string table, dynamic obj)
        {
            SqlQry = "DELETE " + table + " WHERE ID = @ID;";
            try
            {
                SqlCmd = new SqlCommand(SqlQry, Connection);
                SqlCmd.Parameters.Add("@ID" , SqlDbType.Int);
                SqlCmd.Parameters["@ID"].Value = obj.ID;
                SqlCmd.ExecuteNonQuery();
            }
            catch { MainVM.List[0].LogCurrentText = "Delete object from SQL table '" + table + "' failed!"; }
        }

        public static void AddSQL(string table, dynamic obj)
        {
            Dictionary<PropertyInfo, dynamic> dict = obj.ReturnAsDict(false, false, false, true);
            SqlQry = "";
            SqlQry += "INSERT INTO " + table + " ( ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + ") VALUES ( ";
            foreach (PropertyInfo key in dict.Keys) { SqlQry += "@" + key.Name + ", "; }
            SqlQry = SqlQry.Substring(0, SqlQry.Length - 2) + ")";
            try
            {
                SqlCmd = new SqlCommand(SqlQry, Connection);
                foreach (PropertyInfo key in dict.Keys)
                {
                    SqlCmd.Parameters.Add("@" + key.Name, GetSqlDbType(dict[key].GetType()));
                    SqlCmd.Parameters["@" + key.Name].Value = dict[key];
                }
                SqlCmd.ExecuteNonQuery();
            }
            catch { MainVM.List[0].LogCurrentText = "Insert object to SQL table '" + table + "' failed!"; }
        }

        public static void ReseedSQL(string table, int value)
        {
            SqlQry = "DBCC CHECKIDENT(" + table + ", RESEED, " + value.ToString() + ");";
            try
            {
                SqlCmd = new SqlCommand(SqlQry, Connection);
                SqlCmd.ExecuteNonQuery();
            }
            catch { MainVM.List[0].LogCurrentText = "Reseeding SQL table '" + table + "' failed!"; }
        }
    }
}
