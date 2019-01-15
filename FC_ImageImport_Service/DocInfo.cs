using System.Collections.Generic;

namespace FCXml
{
    public class Document
    {
        public Document(string id,string namedb,string speed,string opr,string spo,string sdivision,string spfname)
        {
            ID = id;
            SPFNUM = sdivision;
            SPEED_FL = speed.Trim();
            OPR_TYPE = opr.Trim();
            SPO_TYPE = spo;
            SPF = spfname.Replace("\"", "").Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("*", "").Replace("?", "").Replace(":", "").Replace("|", "");
            NameDB = namedb.Replace("\"", "").Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("*", "").Replace("?", "").Replace(":", "").Replace("|", "");

            int i = NameDB.LastIndexOf('.');
            if (i > 0)
            {
                NameHF = SPF + "_" + ID + NameDB.Substring(NameDB.LastIndexOf('.'));
            }
            else
            {
                NameHF = SPF + "_" + ID;
            }

            NameXML = SPF + "_" + ID + ".xml";
            AnnexPages = new List<Annex>();
            Swift = new List<Swift>();
            SwiftLists = new List<SwiftList>();
        }

        //значения свойств из базы, описание баз в папке source
        public string ID;
        public string SPEED_FL;
        public string OPR_TYPE;
        public string SPO_TYPE;
        public string SPFNUM;
        public string SPF;
        public string ACCOUNT_COMM;
        public string NameDB; //filename from db имя оригинала
        public string NameHF; //filename in hotfolder имя в горяей папке
        public string NameXML;//filename in hotfolder имя файла описания в горяей папке

        public List<Annex> AnnexPages;
        public List<Swift> Swift;
        public List<SwiftList> SwiftLists;
    }
    public class Annex
    {
        public Annex(string id, string parent_id, string namedb, string SPF)
        {
            ID = id;
            PID = parent_id;
            NameDB = namedb.Replace("\"", "").Replace("\\", "").Replace("/", "").Replace("<", "").Replace(">", "").Replace("*", "").Replace("?", "").Replace(":", "").Replace("|", "");

            int i = NameDB.LastIndexOf('.');
            if (i > 0)
            {
                NameHF = SPF + "_" + PID + "_" + ID + NameDB.Substring(NameDB.LastIndexOf('.'));
            }
            else
            {
                NameHF = SPF + "_" + PID + "_" + ID;
            }



        }
        public string PID;
        public string ID;     //Id from db
        public string NameDB; //filename from db
        public string NameHF; //filename in hotfolder
    }

    public class Swift
    {
        public Swift(string number, string date, string iinPayer, string iikPayer, string namePayer, 
            string bikPayer, string bikRecip, string iinRecip, string iikRecip, string nameRecip, string assign, string knp,
            string kod, string kbe, string chief, string mainbk, string totalSum, string periodMain)
        {
            this.number = number;
            this.date = date;
            this.iinPayer = iinPayer;
            this.iikPayer = iikPayer;
            this.namePayer = namePayer;
            this.bikPayer = bikPayer;
            this.bikRecip = bikRecip;
            this.iinRecip = iinRecip;
            this.iikRecip = iikRecip;
            this.nameRecip = nameRecip;
            this.assign = assign;
            this.knp = knp;
            this.kod = kod;
            this.kbe = kbe;
            this.chief = chief;
            this.mainbk = mainbk;
            this.totalSum = totalSum;
            this.periodMain = periodMain;
        }

        public string number = "";
        public string date = "";
        public string iinPayer = "";
        public string iikPayer = "";
        public string namePayer = "";
        public string bikPayer = "";

        public string bikRecip = "";
        public string iinRecip = "";
        public string iikRecip = "";
        public string nameRecip = "";
        public string assign = "";
        public string knp = "";
        public string irsRecip = "";
        public string secoRecip = "";
        public string irsPayer = "";
        public string secoPayer = "";
        public string kod = "";
        public string kbe = "";

        public string chief = "";
        public string mainbk = "";

        public string totalSum = "";
        public string periodMain = "";
    }

    public class SwiftList
    {
        public SwiftList(string number, string sum, string fm, string dt, string iik, string iin, string period)
        {
            this.number = number;
            this.sum = sum;
            this.fm = fm;
            this.dt = dt;
            this.iik = iik;
            this.iin = iin;
            this.period = period;
        }

        public string number = ":21:";
        public string sum = ":32B:";
        public string fm = "/FM/";
        public string nm = "/NM/";
        public string ft = "/FT/";
        public string dt = "/DT/";
        public string iin = "/IDN/";
        public string iik = "/LA/";
        public string period = "/PERIOD/";
    }
}
