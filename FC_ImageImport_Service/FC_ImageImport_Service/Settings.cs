using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace OracleLib
{
    /// <summary>
    /// Настройки сервиса 
    /// Импорт настроек производится из файла конфига один раз на старте
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Папки импорта для FC (горячие папки)
        /// </summary>
        public string Payments_Normal_Folder;
        public string Payments_Urgent_Folder;
        public string Payments_NextDay_Folder;
        public string Paymets_Internal_Folder;
        public string Valute_Folder;
        public string Payments_SWIFT_Folder;

        /// <summary>
        /// ID отделений для импорта изобраджений из базы.
        /// Отдельный список для каждого типа документа
        /// </summary>
        private string Divisions_Standard; //для обычных плтажных поручений
        private string Division_Valute;    //для валютных платежей
        private string Divions_Lists;      //для платежей с  списками

        /// <summary>
        /// SQL запросы к базе Oracle Банка для выбора записей с бинарными данными изображений
        /// </summary>
        private string SelectStandard;
        private string SelectValute;
        private string SelectStandardSpisok;
        private string SelectSwift;

        /// <summary>
        /// Комманды чтения из БД
        /// </summary>
        public OracleCommand SelectStandardCommand;
        public OracleCommand SelectValuteCommand;
        public OracleCommand SelectStandardSpisokCommand;
        public OracleCommand SelectSwiftCommand;
       

        private OracleConnectionStringBuilder ORA_STRB;
        public OracleConnection ORA_Connection;
        public SqlConnection SQL_Connection;
       
        public Settings()
        {
            GetConnectionStrings();
            GetFolders();
            GetDevisions();

            //Описание базы см в папке source
            //status=2 означает что документ готов к обработке MT-TYPE-тип документа

            SelectStandard      = "SELECT PFILE,FILENAME,ID,SDIVISION,SPEEDFL,OPR_TYPE,SPO_TYPE  from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1   AND STATUS='2' AND SDIVISION IN (" +Divisions_Standard + ") AND MT_TYPE='100' order by ID desc ";
            SelectValute        = "SELECT PFILE,FILENAME,ID,SDIVISION,SPEEDFL,OPR_TYPE,SPO_TYPE,ACCOUNT_COMM  from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1   AND STATUS='2' AND SDIVISION IN (" + Division_Valute + ") AND MT_TYPE='103' order by ID desc ";
            SelectStandardSpisok= "SELECT PFILE,FILENAME,ID,SDIVISION,SPEEDFL,OPR_TYPE,SPO_TYPE  from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1 AND SID is NULL  AND STATUS='2' AND SDIVISION IN (" + Divions_Lists + ") AND MT_TYPE='102' order by ID desc ";
            SelectSwift         = "SELECT PFILE,FILENAME,ID,SDIVISION,SPEEDFL,OPR_TYPE,SPO_TYPE  from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1 AND SID is NULL  AND STATUS='2' AND SDIVISION IN (" + Divions_Lists + ") AND MT_TYPE='104' order by ID desc ";

            InitializeReaderCommands(ref SelectStandardCommand, SelectStandard);
            Trace.TraceInformation("Get StandardCommand");
            InitializeReaderCommands(ref SelectValuteCommand, SelectValute);
            Trace.TraceInformation("Get ValuteCommand");
            InitializeReaderCommands(ref SelectStandardSpisokCommand, SelectStandardSpisok);
            Trace.TraceInformation("Get SpisokCommand");
            InitializeReaderCommands(ref SelectSwiftCommand, SelectSwift);
            Trace.TraceInformation("Get SwiftCommand");
        }

        private void InitializeReaderCommands(ref OracleCommand com, string selectquery)
        {
            com = new OracleCommand(selectquery, ORA_Connection);
            com.CommandType = CommandType.Text;
            com.BindByName = true;
            com.Parameters.Add(":date1", OracleDbType.Date, DateTime.Now.Date, ParameterDirection.Input);
        }
        private void GetConnectionStrings()
        {
            ConnectionStringSettingsCollection settings = ConfigurationManager.ConnectionStrings;
            var Settings = ConfigurationManager.AppSettings;

            ORA_STRB = new OracleConnectionStringBuilder();
            ORA_STRB.DataSource = Settings["ORA_DOCUMENT_SOURCE"].ToString();
            ORA_STRB.UserID = Settings["ORA_LOGIN"].ToString();
            ORA_STRB.Password = Settings["ORA_PASSWORD"].ToString();

            SQL_Connection = new SqlConnection(Settings["SQLConectionString"].ToString());
            ORA_Connection = new OracleConnection(ORA_STRB.ToString());

            Trace.TraceInformation("Get ConnectionStrings");
        }   
        private void GetFolders()
        {
            var Settings = ConfigurationManager.AppSettings;
            if (Directory.Exists(Settings["PAYMENTS_NEXTDAY"]))
            {
                Payments_NextDay_Folder = Settings["PAYMENTS_NEXTDAY"].ToString();
            }
            else
            {
                Exception ex = new Exception("HotFolder for nextday paymentsdoesn't exist or unspecified");
                throw ex;
            }
            if (Directory.Exists(Settings["PAYMENTS_INTERNAL"]))
            {
                Paymets_Internal_Folder = Settings["PAYMENTS_INTERNAL"].ToString();
            }
            else
            {
                Exception ex = new Exception("HotFolder internal payments doesn't exist or unspecified");
                throw ex;
            }
            if (Directory.Exists(Settings["VALUTE"]))
            {
                Valute_Folder = Settings["VALUTE"].ToString();
            }
            else
            {
                Exception ex = new Exception("HotFolder for valute doesn't exist or unspecified");
                throw ex;
            }
            if (Directory.Exists(Settings["PAYMENTS_URGENT"]))
            {
                Payments_Urgent_Folder = Settings["PAYMENTS_URGENT"].ToString();
            }
            else
            {
                Exception ex = new Exception("HotFolder for urgent payments doesn't exist or unspecified");
                throw ex;
            }
            if (Directory.Exists(Settings["PAYMENTS_NORMAL"]))
            {
                Payments_Normal_Folder = Settings["PAYMENTS_NORMAL"].ToString();
            }
            else
            {
                Exception ex = new Exception("HotFolder normal payments doesn't exist or unspecified");
                throw ex;
            }

            Trace.TraceInformation("Get Folders");
        }

        private void GetDevisions()
        {
            var Settings = ConfigurationManager.AppSettings;
            Divisions_Standard = Settings["DEVISIONS_Standard"].ToString();
            Division_Valute = Settings["DEVISIONS_Valute"].ToString();
            Divions_Lists = Settings["DEVISIONS_Lists"].ToString();

            Trace.TraceInformation("Get Divisions");
        }
        public void TestSqlConnection()
        {
            try
            {
                SQL_Connection.Open();
                Trace.TraceInformation("Connection test succeeded for SQL");
            }
            catch
            {
                Exception ex = new Exception("Connection test failed for SQL");
                throw ex;         
            }
            finally
            {
                SQL_Connection.Close();
            }

        }
        public void TestOracleConnection()
        {

            try
            {
                ORA_Connection.Open();
                Trace.TraceInformation("Connection test succeeded for:" + ORA_STRB.DataSource);
            }
            catch
            {
                Exception ex = new Exception("Connection test failed for Oracle");
                throw ex;
            }
            finally
            {
                ORA_Connection.Close();
            }
        }
    }
}
