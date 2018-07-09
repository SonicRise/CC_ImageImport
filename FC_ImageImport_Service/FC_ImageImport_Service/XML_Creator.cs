using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace FCXml
{
    public static class CFlexiCaptureOperator
    {
       

        /// <summary>
        /// Creates an object for serializing to a batch Xml.
        /// генератор файла XML описания
        /// </summary>
        public static Batch CreateBatchXml(Document Doc)
        {
            string NamingShema =Doc.SPF + "_" + Doc.ID;
            FlexiCaptureExportXml exportXml = new FlexiCaptureExportXml();
            Batch batch = new Batch();
            batch.Name = NamingShema;

            List<BatchDocument> fcDocuments = new List<BatchDocument>();
            BatchDocument fcDocument = new BatchDocument();
            fcDocument.Index = "0";
            fcDocument.Name = Doc.ID;

            List<BatchDocumentParameter> parameters = new List<BatchDocumentParameter>();

            BatchDocumentParameter Param1 = new BatchDocumentParameter();
            Param1.Name = "СПФ";
            Param1.Value = Doc.SPF;
            parameters.Add(Param1);

            BatchDocumentParameter Param2 = new BatchDocumentParameter();
            Param2.Name = "ID";
            Param2.Value = Doc.ID;
            parameters.Add(Param2);


            BatchDocumentParameter Param3 = new BatchDocumentParameter();
            Param3.Name = "SPEEDFL";
            Param3.Value = Doc.SPEED_FL;
            parameters.Add(Param3);

            BatchDocumentParameter Param4 = new BatchDocumentParameter();
            Param4.Name = "OPR_TYPE";
            Param4.Value = Doc.OPR_TYPE;
            parameters.Add(Param4);

            BatchDocumentParameter Param5 = new BatchDocumentParameter();
            Param5.Name = "SPO_TYPE";
            Param5.Value = Doc.SPO_TYPE;
            parameters.Add(Param5);

            BatchDocumentParameter Param6 = new BatchDocumentParameter();
            Param6.Name = "FileName";
            Param6.Value = Doc.NameDB;
            parameters.Add(Param6);
            
            BatchDocumentParameter Param7 = new BatchDocumentParameter();
            Param7.Name = "ACCOUNT_COM";
            Param7.Value = Doc.ACCOUNT_COMM;
            parameters.Add(Param7);
               

            List<BatchDocumentImage> images = new List<BatchDocumentImage>();
            BatchDocumentImage image = new BatchDocumentImage();
            image.Index = "0";
            image.Value = Doc.NameHF;
            images.Add(image);
            

            for (int i = 0; i < Doc.AnnexPages.Count; i++)
            {
                BatchDocumentParameter Param = new BatchDocumentParameter();
                Param.Name = "ID_REESTR_" + (i + 1).ToString();
                Param.Value = Doc.AnnexPages[i].ID;
                parameters.Add(Param);
                               
                BatchDocumentImage Pageimage = new BatchDocumentImage();
                Pageimage.Index = (i + 1).ToString();
                Pageimage.Value = Doc.AnnexPages[i].NameHF;
                images.Add(Pageimage);
            }

            Trace.TraceInformation("Doc.Swift.Count: " + Doc.Swift.Count);

            if(Doc.Swift.Count == 1)
            {
                BatchDocumentParameter Param10 = new BatchDocumentParameter();
                Param1.Name = "Number";
                Param1.Value = Doc.Swift[0].number;
                parameters.Add(Param1);

                BatchDocumentParameter Param11 = new BatchDocumentParameter();
                Param11.Name = "Date";
                Param11.Value = Doc.Swift[0].date;
                parameters.Add(Param11);

                BatchDocumentParameter Param12 = new BatchDocumentParameter();
                Param12.Name = "IINPayer";
                Param12.Value = Doc.Swift[0].iinPayer;
                parameters.Add(Param12);

                BatchDocumentParameter Param13 = new BatchDocumentParameter();
                Param13.Name = "IIKPayer";
                Param13.Value = Doc.Swift[0].iikPayer;
                parameters.Add(Param13);

                BatchDocumentParameter Param14 = new BatchDocumentParameter();
                Param14.Name = "NamePayer";
                Param14.Value = Doc.Swift[0].namePayer;
                parameters.Add(Param14);

                BatchDocumentParameter Param15 = new BatchDocumentParameter();
                Param15.Name = "BIKPayer";
                Param15.Value = Doc.Swift[0].bikPayer;
                parameters.Add(Param15);

                BatchDocumentParameter Param16 = new BatchDocumentParameter();
                Param16.Name = "BIKRecip";
                Param16.Value = Doc.Swift[0].bikRecip;
                parameters.Add(Param16);

                BatchDocumentParameter Param17 = new BatchDocumentParameter();
                Param17.Name = "IINRecip";
                Param17.Value = Doc.Swift[0].iinRecip;
                parameters.Add(Param17);

                BatchDocumentParameter Param18 = new BatchDocumentParameter();
                Param18.Name = "IIKRecip";
                Param18.Value = Doc.Swift[0].iikRecip;
                parameters.Add(Param18);

                BatchDocumentParameter Param19 = new BatchDocumentParameter();
                Param19.Name = "NameRecip";
                Param19.Value = Doc.Swift[0].nameRecip;
                parameters.Add(Param19);

                BatchDocumentParameter Param20 = new BatchDocumentParameter();
                Param20.Name = "Assign";
                Param20.Value = Doc.Swift[0].assign;
                parameters.Add(Param20);

                BatchDocumentParameter Param21 = new BatchDocumentParameter();
                Param21.Name = "KNP";
                Param21.Value = Doc.Swift[0].knp;
                parameters.Add(Param21);

                BatchDocumentParameter Param22 = new BatchDocumentParameter();
                Param22.Name = "KOD";
                Param22.Value = Doc.Swift[0].kod;
                parameters.Add(Param22);

                BatchDocumentParameter Param23 = new BatchDocumentParameter();
                Param23.Name = "KBE";
                Param23.Value = Doc.Swift[0].kbe;
                parameters.Add(Param23);

                BatchDocumentParameter Param24 = new BatchDocumentParameter();
                Param24.Name = "CHIEF";
                Param24.Value = Doc.Swift[0].chief;
                parameters.Add(Param24);

                BatchDocumentParameter Param25 = new BatchDocumentParameter();
                Param25.Name = "MAINBK";
                Param25.Value = Doc.Swift[0].mainbk;
                parameters.Add(Param25);

                BatchDocumentParameter Param26 = new BatchDocumentParameter();
                Param26.Name = "NumberOfSwift";
                Param26.Value = Doc.SwiftLists.Count.ToString();
                parameters.Add(Param26);

                BatchDocumentParameter Param27 = new BatchDocumentParameter();
                Param27.Name = "TotalSum";
                Param27.Value = Doc.Swift[0].totalSum;
                parameters.Add(Param27);

                BatchDocumentParameter Param28 = new BatchDocumentParameter();
                Param28.Name = "PeriodMain";
                Param28.Value = Doc.Swift[0].periodMain;
                parameters.Add(Param28);


                for (int i = 0; i < Doc.SwiftLists.Count; i++)
                {
                    BatchDocumentParameter ParamS1 = new BatchDocumentParameter();
                    ParamS1.Name = "Number_swift" + (i + 1).ToString();
                    ParamS1.Value = Doc.SwiftLists[i].number;
                    parameters.Add(ParamS1);

                    BatchDocumentParameter ParamS2 = new BatchDocumentParameter();
                    ParamS2.Name = "Sum_swift" + (i + 1).ToString();
                    ParamS2.Value = Doc.SwiftLists[i].sum;
                    parameters.Add(ParamS2);

                    BatchDocumentParameter ParamS3 = new BatchDocumentParameter();
                    ParamS3.Name = "FM_swift" + (i + 1).ToString();
                    ParamS3.Value = Doc.SwiftLists[i].fm;
                    parameters.Add(ParamS3);

                    BatchDocumentParameter ParamS4 = new BatchDocumentParameter();
                    ParamS4.Name = "DT_swift" + (i + 1).ToString();
                    ParamS4.Value = Doc.SwiftLists[i].dt;
                    parameters.Add(ParamS4);

                    BatchDocumentParameter ParamS5 = new BatchDocumentParameter();
                    ParamS5.Name = "IIK_swift" + (i + 1).ToString();
                    ParamS5.Value = Doc.SwiftLists[i].iik;
                    parameters.Add(ParamS5);

                    BatchDocumentParameter ParamS6 = new BatchDocumentParameter();
                    ParamS6.Name = "IIN_swift" + (i + 1).ToString();
                    ParamS6.Value = Doc.SwiftLists[i].iin;
                    parameters.Add(ParamS6);

                    BatchDocumentParameter ParamS7 = new BatchDocumentParameter();
                    ParamS7.Name = "PERIOD_swift" + (i + 1).ToString();
                    ParamS7.Value = Doc.SwiftLists[i].period;
                    parameters.Add(ParamS7);
                }
            }

            fcDocument.Images = images.ToArray();
            fcDocument.RegistrationParams = parameters.ToArray();
            fcDocuments.Add(fcDocument);
            batch.Documents = fcDocuments.ToArray();
            return batch;
        }

        /// <summary>
        /// Puts a batch into the FlexiCapture hot folder.
        /// </summary>
        public static void SendBatchToFlexiCapture(Document Doc,string HotFolder)
        {
          
            string NamingShema =  Doc.SPF + "_" + Doc.ID;
            Batch exportBatch = CreateBatchXml(Doc);
            XmlSerializer serializer = new XmlSerializer(typeof(Batch));
            FileStream fs = File.OpenWrite(Path.Combine(HotFolder, NamingShema + ".xml"));
            serializer.Serialize(fs, exportBatch);
            fs.Close();

        }
    }
}
