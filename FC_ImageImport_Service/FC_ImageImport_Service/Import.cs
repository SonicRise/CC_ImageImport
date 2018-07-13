using System;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Configuration;
using FCXml;
using System.Threading;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OracleLib
{
    public class OracleConnector
    {
        private CancellationTokenSource CTS;
        private int exceptioncount;

        public Settings ServiceSettings;
        public OracleConnector(CancellationTokenSource Source)
        {
            CTS = Source;
            ServiceSettings = new Settings();
            ServiceSettings.TestOracleConnection();
            ServiceSettings.TestSqlConnection();
        }
        public void Import(OracleCommand Com, ImportType Type)
        {

            string importFolder;
            try
            {
                ServiceSettings.ORA_Connection.Open();
                ServiceSettings.SQL_Connection.Open();

                using (OracleDataReader reader = Com.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //получаем имя отделения 
                            string divisionname = GetDevisionName(reader["SDIVISION"].ToString());

                            //заполянеям свойства документа для дальнейшего использования
                            Document doc = new Document(id: reader["ID"].ToString(),
                                                        namedb: reader["FILENAME"].ToString(),
                                                        speed: reader["SPEEDFL"].ToString(),
                                                        opr: reader["OPR_TYPE"].ToString(),
                                                        spo: reader["SPO_TYPE"].ToString(),
                                                        sdivision: reader["SDIVISION"].ToString(),
                                                        spfname: divisionname);

                            //определяем куда сохранить файл
                            //если импортируется стандартный палтёж или со списком
                            if (Type != ImportType.Valute)
                            {
                                importFolder = GetImportFolder(doc);
                                Trace.TraceInformation("!=Valute GetImportFolder: " + importFolder);
                            }
                            //если импортируется валютный палтёж
                            else
                            {
                                importFolder = ServiceSettings.Valute_Folder;
                                doc.ACCOUNT_COMM = reader["ACCOUNT_COMM"].ToString();
                                Trace.TraceInformation("=Valute GetImportFolder: " + importFolder);
                            }
                            //в случае если импортируется платёж с списком
                            if (Type == ImportType.StandardSpisok)
                            {
                                WriteList(doc, importFolder);
                                Trace.TraceInformation("WriteList function finished");
                            }

                            if (Type == ImportType.Swift)
                            {
                                WriteSwift(doc, importFolder);
                                Trace.TraceInformation("WriteSwift function finished");
                            }

                            //Cохраняем изображение и обновляем значение в базе
                            WriteMainDoc(reader, doc, importFolder);
                            Trace.TraceInformation("WriteMainDoc finished");
                            //Создаём xml файл описания
                            CreateXML(doc, importFolder);
                            Trace.TraceInformation("CreateXML finished");

                        }
                    }
                    else
                    {
                       // Trace.TraceInformation("No rows in a table.");
                    }
                }
            }
            catch (Exception ex)
            {
                CheckExceptionCount(ex, Type);
            }
            finally
            {
                ServiceSettings.ORA_Connection.Close();
                ServiceSettings.SQL_Connection.Close();
            }

        }
        private string GetImportFolder(Document Doc)
        {
            //OPR type берётся из базы, см описания баз
            //папки задаются в конфиге и считываются на старте
            if (Doc.OPR_TYPE == "02015")   //02015 - opr type след день
            {
                return ServiceSettings.Payments_NextDay_Folder;
            }
            else if (Doc.OPR_TYPE == "01")   //01 - opr type внутри банковский
            {
                return ServiceSettings.Paymets_Internal_Folder;
            }
            else                                  //обычные
            {

                if (string.IsNullOrEmpty(Doc.SPEED_FL) || (Doc.SPEED_FL == "0"))
                {
                    return ServiceSettings.Payments_Normal_Folder;
                }
                else                             //срочные
                {
                    return ServiceSettings.Payments_Urgent_Folder;
                }
            }
        }
        private void WriteMainDoc(OracleDataReader reader, Document Doc, string ImportFolder)
        {
            //сохранение основоного документа в случае списков или просто документа в случае когда списков нет
            Trace.TraceInformation("WriteMainDoc started");
            try
            {
                //сохраняем изображение
                WritePage(reader, Doc.NameHF, ImportFolder);
                Trace.TraceInformation("Image has to be saved to: " + ImportFolder);
                //Обновляем статус 13- значит находится в обработке в FC
                UpdateStatus(reader["ID"].ToString(), 13);
                Trace.TraceInformation("Status has to be updated");
            }
            catch (Exception ex)
            {
                //если что-то пошло не так
                Trace.TraceError("Ошибка импорте изображения ID=" + Doc.ID + ". " + ex.Message);
                //удаляем файл если он есть
                DeleteFiles(Doc, ImportFolder);
                //обновляем статус обратно
                UpdateStatus(reader["ID"].ToString(), 2);
                throw;
            }
        }
        private void WriteList(Document Doc, string ImportFolder)
        {
            // в случае списков необходимо получить дочерние файлы изобрадежений относящиейся к основному документу
            string SelectLists = "SELECT PFILE,FILENAME,ID from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1 AND SID='" + Doc.ID + "' order by ID asc ";
            OracleCommand SelectListsCommand = new OracleCommand(SelectLists, ServiceSettings.ORA_Connection);
            SelectListsCommand.CommandType = CommandType.Text;
            SelectListsCommand.BindByName = true;
            SelectListsCommand.Parameters.Add(":date1", OracleDbType.Date, DateTime.Now.Date, ParameterDirection.Input);
            List<string> FailedImport = new List<string>();

            Trace.TraceInformation("Spisok was found, WriteList(Doc,ImportFolder) is implementing");

            using (OracleDataReader ListsReader = SelectListsCommand.ExecuteReader())
            {
                while (ListsReader.Read())
                {
                    try
                    {
                        Doc.AnnexPages.Add(new Annex(ListsReader["ID"].ToString(), Doc.ID, ListsReader["FILENAME"].ToString(), Doc.SPF));
                        Trace.TraceInformation("Page is added to Annex");
                        WritePage(ListsReader, Doc.AnnexPages[Doc.AnnexPages.Count - 1].NameHF, ImportFolder);
                        Trace.TraceInformation("Image has to be saved to: " + ImportFolder);
                        UpdateStatus(ListsReader["ID"].ToString(), 13);
                        Trace.TraceInformation("Status has to be updated");
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Ошибка во время импорта списка ID=" + ListsReader["ID"].ToString() + ". ID родительского документа:" + "Doc.ID." + ex.Message);
                        FailedImport.Add(ListsReader["ID"].ToString());
                        UpdateStatus(ListsReader["ID"].ToString(), 2);
                    }

                }

            }
            if (FailedImport.Count > 0)
            {
                DeleteFiles(Doc, ImportFolder);
                Trace.TraceInformation("File was deleted after error.");
            }
        }

        private void WriteSwift(Document Doc, string ImportFolder)
        {
            // в случае списков со Свифтом необходимо получить дочерние файлы изобрадежений относящиейся к основному документу
            string SelectLists = "SELECT PFILE,FILENAME,ID from  AC_EXCHANGE_DOCUMENT where rownum < 10000 AND DATEINS>=:date1 AND SID='" + Doc.ID + "' order by ID asc ";
            OracleCommand SelectListsCommand = new OracleCommand(SelectLists, ServiceSettings.ORA_Connection);
            SelectListsCommand.CommandType = CommandType.Text;
            SelectListsCommand.BindByName = true;
            SelectListsCommand.Parameters.Add(":date1", OracleDbType.Date, DateTime.Now.Date, ParameterDirection.Input);
            List<string> FailedImport = new List<string>();

            Trace.TraceInformation("Spisok was found, WriteSwift(Doc,ImportFolder) is implementing");

            using (OracleDataReader ListsReader = SelectListsCommand.ExecuteReader())
            {
                while (ListsReader.Read())
                {
                    try
                    {
                        if (ListsReader["FILENAME"].ToString().Substring(ListsReader["FILENAME"].ToString().Length - 4) == ".pdf" ||
                            ListsReader["FILENAME"].ToString().Substring(ListsReader["FILENAME"].ToString().Length - 4) == ".jpg" || 
                            ListsReader["FILENAME"].ToString().Substring(ListsReader["FILENAME"].ToString().Length - 4) == ".JPG" || 
                            ListsReader["FILENAME"].ToString().Substring(ListsReader["FILENAME"].ToString().Length - 4) == ".PDF" )
                        {
                            Trace.TraceInformation("Filename: " + ListsReader["FILENAME"].ToString());
                            Doc.AnnexPages.Add(new Annex(ListsReader["ID"].ToString(), Doc.ID,ListsReader["FILENAME"].ToString(), Doc.SPF));
                            WritePage(ListsReader, Doc.AnnexPages[Doc.AnnexPages.Count - 1].NameHF, ImportFolder);

                            Trace.TraceInformation("Написали страницы со списком");
                        }
                        else
                        {
                            Trace.TraceInformation("Filename: " + ListsReader["FILENAME"].ToString());
                            GetSwiftData(ListsReader, Doc);
                            Trace.TraceInformation("Закончили фукнцию GetSwiftData(ListsReader, Doc)");
                        }

                        UpdateStatus(ListsReader["ID"].ToString(), 13);
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceError("Ошибка во время импорта списка ID=" + ListsReader["ID"].ToString() + ". ID родительского документа:" + "Doc.ID." + ex.Message);
                        FailedImport.Add(ListsReader["ID"].ToString());
                        UpdateStatus(ListsReader["ID"].ToString(), 2);
                    }

                }

            }
            if (FailedImport.Count > 0)
            {
                DeleteFiles(Doc, ImportFolder);
            }
        }

        private void WritePage(OracleDataReader reader, string filename, string ImportFolder)
        {
            
            Trace.TraceInformation("Сохраняем файл в папку , его имя: " + filename);
            OracleBlob blob = reader.GetOracleBlob(0);
            byte[] buffer = new byte[blob.Length];
            reader.GetBytes(0, 0, buffer, 0, buffer.Length);
            File.WriteAllBytes(ImportFolder + filename, buffer);

            if (filename.Substring(filename.Length - 4) == ".pdf" || filename.Substring(filename.Length - 4) == ".jpg")
            {

            }
            else
            {
                /*string swiftText = Encoding.UTF8.GetString(buffer);

                string[] real = new string[] { "ә", "ң", "ғ", "ү", "ұ", "қ", "ө", "һ", "Ә", "Ң", "Ғ", "Ү", "Ұ", "Қ", "Ө", "Һ" };
                string[] replaced = new string[] { "ј", "ѕ", "є", "ї", "ў", "ќ", "ґ", "ћ", "Ј", "Ѕ", "Є", "Ї", "Ў", "Ќ", "Ґ", "Ћ" };
                for (int i = 0; i < real.Length; i++)
                {
                    swiftText = swiftText.Replace(real[i], replaced[i]);
                }
                Trace.TraceInformation("Swift to string: " + swiftText);*/
            }
            

        }
        private void DeleteFiles(Document Doc, string ImportFolder)
        {
            Trace.TraceInformation("попал в удаление файла");
            if (File.Exists(ImportFolder + Doc.NameHF))
            {
                File.Delete(ImportFolder + Doc.NameHF);
            }

            foreach (Annex page in Doc.AnnexPages)
            {
                if (File.Exists(ImportFolder + page.NameHF))
                {
                    File.Delete(ImportFolder + page.NameHF);
                }
            }
        }

        private void GetSwiftData(OracleDataReader reader, Document Doc)
        {
            Trace.TraceInformation("Начали фукнцию GetSwiftData(ListsReader, Doc)");

            OracleBlob blob = reader.GetOracleBlob(0);
            byte[] buffer = new byte[blob.Length];
            reader.GetBytes(0, 0, buffer, 0, buffer.Length);

            string swiftText = Encoding.Default.GetString(buffer);

            if (swiftText.Contains("Ѓ"))
            {
                swiftText = Encoding.UTF8.GetString(buffer);
            }

            if (!swiftText.Contains("/NUM/"))
            {
                swiftText = Encoding.Unicode.GetString(buffer);
            } 

            string[] real = new string[] { "ј", "ѕ", "є", "ї", "ў", "ќ", "ґ", "ћ", "Ј", "Ѕ", "Є", "Ї", "Ў", "Ќ", "Ґ", "Ћ" };
            string[] replaced = new string[] { "ә", "ң", "ғ", "ү", "ұ", "қ", "ө", "һ", "Ә", "Ң", "Ғ", "Ү", "Ұ", "Қ", "Ө", "Һ" };
            for (int i = 0; i < real.Length; i++)
            {
                swiftText = swiftText.Replace(real[i], replaced[i]);
            }

            Trace.TraceInformation("Переписали файл в стринг");
            
            //Trace.TraceInformation(swiftText);

            string[] list;

            List<string> list1 = new List<string>();

            string numberInList = "";
            string sum = "";
            string fm = "";
            string nm = "";
            string ft = "";
            string dt = "";
            string iik = "";
            string iin = "";
            string period = "";

            string totalSum = "";
            string valDate = "";

            string fmAll = "";



            //разделили основную часть и списки
            string[] main_plus_swift = swiftText.Split(new[] { ":21:" }, StringSplitOptions.None);

            for (int i = 1; i != main_plus_swift.Length; i++)
            {
                main_plus_swift[i] = ":21:" + main_plus_swift[i];
            }


            Trace.TraceInformation("Разделили по :21:");

            //Основной документ
            string[] mainDoc = main_plus_swift[0].Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            Trace.TraceInformation("Разделили первую часть по строкам");
            Trace.TraceInformation("Длина main_plus_swift: " + main_plus_swift.Length);
            //делим информацию по каждому человеку в списке. 
            for (int i = 1; i != main_plus_swift.Length; i++)
            {
                Trace.TraceInformation("Я зашел в цикл по перебору списка");
                list = main_plus_swift[i].Split(new[] { Environment.NewLine }, StringSplitOptions.None);

                Trace.TraceInformation("потом разделил Iтый список по строкам");
                Trace.TraceInformation("list.length: " + list.Length);

                for (int j = 0; j != list.Length - 1; j++)
                {
                    Trace.TraceInformation("Зашел в цикл по перебору строк в Iтом списке");

                    Trace.TraceInformation("list " + j + ": " + list[j] + ", "); //list[j].Substring(0, 4): :32B:KZT600,00

                    if (list[j].Substring(0,1).Equals("-"))
                    {
                        break;
                    }
                    else if(list[j].Length < 4){
                        //пропуск строки, в которой меньше чем 4 знака.
                    }
                    else
                    {
                        if (list[j].Substring(0, 4).Equals(":21:"))
                        {
                            numberInList = list[j].Substring(4);
                        }
                        else if (list[j].Substring(0, 4).Equals(":32B"))
                        {
                            sum = list[j].Substring(8);
                        }
                        else if (list[j].Substring(0, 4).Equals("/FM/"))
                        {
                            fm = list[j].Substring(4);
                        }
                        else if (list[j].Substring(0, 4).Equals("/NM/"))
                        {
                            nm = list[j].Substring(4);
                        }
                        else if (list[j].Substring(0, 4).Equals("/FT/"))
                        {
                            ft = list[j].Substring(4);
                        }
                        else if (list[j].Substring(0, 4).Equals("/DT/"))
                        {
                            dt = list[j].Substring(4); // /DT/19900806   06.80.00 
                                                       // /DT/1978924    4.24.89  
                                                       // /DT/19770702   02.70.70

                            dt = dt.Substring(6) + "." + dt.Substring(4, 2) + "." + dt.Substring(2, 2);

                        }
                        else if (list[j].Substring(0, 4).Equals("/IDN"))
                        {
                            iin = list[j].Substring(5);
                        }
                        else if (list[j].Substring(0, 4).Equals("/PER"))
                        {
                            period = list[j].Substring(8);
                        }
                        else if (list[j].Substring(0, 4).Equals("/LA/"))
                        {
                            iik = list[j].Substring(4);
                        }
                        else if (list[j].Substring(0, 4).Equals(":32A"))
                        {
                            valDate = list[j].Substring(5, 6);
                            valDate = valDate.Substring(4) + "." + valDate.Substring(2, 2) + "."
                            + valDate.Substring(0, 2);
                            totalSum = list[j].Substring(14);
                        }
                        Trace.TraceInformation(numberInList + " " + sum + " " + fm + " " + dt + " " + iin + " " + period);
                    }
                }

                fmAll = fm + " " + nm + " " + ft;
                Trace.TraceInformation("Перед записывание в Док файл");
                Doc.SwiftLists.Add(new SwiftList(numberInList, sum, fmAll, dt, iik, iin, period));

                numberInList = "";
                sum = "";
                fm = "";
                nm = "";
                ft = "";
                dt = "";
                iik = "";
                iin = "";
                period = "";
                fmAll = "";

                Trace.TraceInformation("Записали Док файл");
            }

            Trace.TraceInformation("Замутили список");

            //работаем с основным документом

            string number = "";
            string date = "";
            string iinPayer = "";
            string iikPayer = "";
            string namePayer = "";
            string bikPayer = "";

            string bikRecip = "";
            string iinRecip = "";
            string iikRecip = "";
            string nameRecip = "";
            string assign = "";
            string knp = "";
            string irsRecip = "";
            string secoRecip = "";
            string irsPayer = "";
            string secoPayer = "";
            string kod = "";
            string kbe = "";

            string chief = "";
            string mainbk = "";
            string periodMain = "";


            Trace.TraceInformation("Начали работу с main doc");
            for (int i = 4; i != mainDoc.Length-1; i++)
            {
                Trace.TraceInformation("mainDoc " + i + ": " + mainDoc[i]);

                if(mainDoc[i].Length >= 4){

                    if (mainDoc[i].Substring(0, 4).Equals("/NUM"))
                    {
                        number = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals(":70:") && mainDoc[i].Length != 4)
                    {
                        number = mainDoc[i].Substring(9);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/DAT"))
                    {
                        date = mainDoc[i].Substring(6);
                        date = date.Substring(4) + "." + date.Substring(2, 2) + "."
                            + date.Substring(0, 2);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/IDN") && iinPayer == "")
                    {
                        iinPayer = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals(":50:"))
                    {
                        iikPayer = mainDoc[i].Substring(7);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/NAM") && namePayer == "")
                    {
                        namePayer = mainDoc[i].Substring(6);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals(":52B"))
                    {
                        bikPayer = mainDoc[i].Substring(5);

                    }
                    else if (mainDoc[i].Substring(0, 4).Equals(":57B"))
                    {
                        bikRecip = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/IDN"))
                    {
                        iinRecip = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals(":59:"))
                    {
                        iikRecip = mainDoc[i].Substring(4);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/NAM"))
                    {
                        nameRecip = mainDoc[i].Substring(6);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/ASS"))
                    {
                        assign = mainDoc[i].Substring(8);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/KNP"))
                    {
                        knp = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/IRS") && irsPayer == "")
                    {
                        irsPayer = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/SEC") && secoPayer == "")
                    {
                        secoPayer = mainDoc[i].Substring(6);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/IRS"))
                    {
                        irsRecip = mainDoc[i].Substring(5);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/SEC"))
                    {
                        secoRecip = mainDoc[i].Substring(6);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/CHI"))
                    {
                        chief = mainDoc[i].Substring(7);
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/MAI"))
                    {
                        mainbk = mainDoc[i].Substring(8);
                        if (mainbk == "")
                        {
                            mainbk = "НЕ ПРЕДУСМОТРЕН";
                        }
                    }
                    else if (mainDoc[i].Substring(0, 4).Equals("/PER"))
                    {
                        periodMain = mainDoc[i].Substring(8);
                    }
                    else if (!mainDoc[i].Substring(0,1).Equals("/") && !mainDoc[i].Substring(0, 1).Equals("{") && !mainDoc[i].Substring(0, 1).Equals(":"))
                    {
                        assign = assign + mainDoc[i];
                    }
                }else{
                    assign = assign + mainDoc[i];
                }

                
                Trace.TraceInformation(number + date + iinPayer + iikPayer + namePayer + bikPayer + iinRecip + iikRecip + nameRecip + assign + knp + kod + kbe + chief + mainbk);
            }
                    
            kod = irsPayer + secoPayer;
            kbe = irsRecip + irsRecip;

            Trace.TraceInformation(kod + " " + kbe);

            Doc.Swift.Add(new Swift(number, date, iinPayer, iikPayer,namePayer,bikPayer,bikRecip, iinRecip,iikRecip,nameRecip, assign, knp, kod, kbe, chief, mainbk, totalSum , periodMain));

            Trace.TraceInformation("Замутили освновной документ");

        }


        /// <summary>
        /// обновление статуса документа в базе Oracle банка
        /// </summary>
        /// <param name="ID">ID записи</param>
        /// <param name="status">новый статус 2-готов к обработке 13-в обработке в FC</param>
        private void UpdateStatus(string ID,int status)
        {
            Trace.TraceInformation("Обновили статус");
            OracleCommand cmd = new OracleCommand("Z_F_UPD_STAT_ACEXCDOC", ServiceSettings.ORA_Connection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("N_ID", OracleDbType.Int32, ParameterDirection.Input).Value = ID;
            cmd.Parameters.Add("N_STATUS ", OracleDbType.Int32, ParameterDirection.Input).Value = status;
            cmd.Parameters.Add("S_REASON  ", OracleDbType.Varchar2, ParameterDirection.Input).Value = "";
            cmd.ExecuteNonQuery();                            
        }

        /// <summary>
        ///  обновление счетчика импортированных документов в специальной базе данных проекта
        /// </summary>
        private void UpdateSQL()
        {
            string select = "SELECT COUNT FROM DOC_COUNTER WHERE DATE='" + DateTime.Now.Date.ToString("dd.MM.yyyy") + "'";
            string insert = "INSERT into DOC_COUNTER VALUES ('" + DateTime.Now.Date.ToString("dd.MM.yyyy") + "','1')";
            string update = "";
            SqlCommand GetCount = new SqlCommand(select, ServiceSettings.SQL_Connection);
            SqlCommand InsertNew = new SqlCommand(insert, ServiceSettings.SQL_Connection);
            object Count = GetCount.ExecuteScalar();
          
                if (Count != null)
                {
                    update = "UPDATE DOC_COUNTER SET COUNT='" + (int.Parse(Count.ToString()) + 1).ToString() + "' WHERE DATE='" + DateTime.Now.Date.ToString("dd.MM.yyyy") + "'";
                    SqlCommand AddOne = new SqlCommand(update, ServiceSettings.SQL_Connection);
                    AddOne.ExecuteNonQuery();
                }
                else
                {
                    InsertNew.ExecuteNonQuery();
                }            
        }  

        /// <summary>
        /// Извлечение имени филиала из базы Oracle банка
        /// </summary>
        /// <param name="sdiviosion">ID филиала</param>
        /// <returns>имя филиала</returns>
        private string GetDevisionName(string sdiviosionID)
        {
            string SelectDevision = "SELECT DESCRIPTION  from  Z_DIVISION where PDIVISION='" + sdiviosionID + "'";
            OracleCommand SelectCommand = new OracleCommand(SelectDevision, ServiceSettings.ORA_Connection);
            SelectCommand.CommandType = CommandType.Text;
            SelectCommand.BindByName = true;

            return SelectCommand.ExecuteScalar().ToString();

        }

        /// <summary>
        /// Создание XML файла описания в папке импорта FC
        /// </summary>
        /// <param name="Doc"></param>
        /// <param name="importFolder"></param>
        private void CreateXML(Document Doc,string importFolder)
        {
            Trace.TraceInformation("CreateXML started");
            try
            {
                if (File.Exists(importFolder + Doc.NameHF))
                {
                    CFlexiCaptureOperator.SendBatchToFlexiCapture(Doc, importFolder);
                    //UpdateSQL();
                }
                else
                {
                    Trace.TraceInformation("File doen't exist");
                }
            }

            catch (Exception ex)
            {
                Trace.TraceError("Ошибка при создани XML описания. ID=" + Doc.ID + ". " + ex.Message);

                DeleteFiles(Doc, importFolder);
                //UpdateStatus(Doc.ID, 2);
                throw;
            }
        }
        private void CheckExceptionCount(Exception ex,ImportType Type)
        {
            //считаем что если воникает мнодество ошибок то целесообразно остановить сервис и выяснить их причины
            exceptioncount++;
                Trace.TraceError("Ошибка выполнения операции импорта платежа"+ Type.ToString()+ ":" + ex.Message+ex.StackTrace);

                if (exceptioncount > 15)
                {
                    CTS.Cancel();
                    Trace.TraceError("Многочисленные ошибки при выполнении операций импорта. Проверьте ошибки и перезапустите сервис.");
                }
        }
    }
    public enum ImportType
    {
        Standard,
        Valute,
        StandardSpisok,
        Swift
    }
}
