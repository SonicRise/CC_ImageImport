using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;

using System.ServiceProcess;

using System.Threading.Tasks;
using System.Threading;
using OracleLib;
namespace FC_ImageImport_Service
{
    public partial class FC_ImageImport_Service : ServiceBase
    {
        private System.Timers.Timer timer;
        private Task ImportTask;
        private CancellationTokenSource CTS;
        private OracleConnector OC;

        private int count = 0;

        public FC_ImageImport_Service()
        {
            InitializeComponent();
            CTS = new CancellationTokenSource();
            ServiceName = "FC_ImageImport_Service";
            timer = new System.Timers.Timer();
            timer.Enabled = true;
            timer.Interval = Convert.ToDouble(1800000);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
            timer.AutoReset = true;
        }

        protected override void OnStart(string[] args)
        {     
            timer.Start();
               
            try
            {
                
                OC = new OracleConnector(CTS);         
            }
            catch 
            {       
                throw;                           
            }
                    
            ImportTask = new Task(() =>
                    {
                        while (CTS.IsCancellationRequested == false)
                        {
                            //запуск заданий импорта изображений из базы данных
                            CTS.Token.ThrowIfCancellationRequested();
                            OC.Import(OC.ServiceSettings.SelectStandardCommand,ImportType.Standard);
                            OC.Import(OC.ServiceSettings.SelectValuteCommand, ImportType.Valute);
                            OC.Import(OC.ServiceSettings.SelectStandardSpisokCommand, ImportType.StandardSpisok);
                            OC.Import(OC.ServiceSettings.SelectSwiftCommand, ImportType.Swift);
                           
                        }

                    }, CTS.Token);
                ImportTask.Start();                
        }
        public void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            //для проверки что с сервисом всё порядке и он работает
            if (CTS.IsCancellationRequested==false)
            {
                Trace.TraceInformation("Service runnnig");
            }
            if (CTS.IsCancellationRequested == true)
            {
                Trace.TraceError("Errors encountered, review log for more information.");
            }

        }
        protected override void OnStop()
        {
            CTS.Cancel();
            
            if (ImportTask != null && ImportTask.Status == TaskStatus.Running )
            {
                ImportTask.Wait();
            }
        }
        protected override void OnShutdown()
        {
            CTS.Cancel();

            if (ImportTask != null && ImportTask.Status == TaskStatus.Running)
            {
                ImportTask.Wait();
            }
        }
    }
}
