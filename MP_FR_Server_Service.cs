using System;
using System.ServiceProcess;
using System.Threading;

using Bend.Util;

namespace MP_FR_Server_Service
{
    public partial class MP_FR_Server_Service : ServiceBase
    {
        public HttpServer httpServer;
        public Thread thread;

        public MP_FR_Server_Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            HttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new MyHttpServer(Convert.ToInt16(args[0]));
            }
            else {
                httpServer = new MyHttpServer(13000);
            }

            thread = new Thread(new ThreadStart(httpServer.listen));
            
            thread.Start();
        }

        protected override void OnStop()
        {
            thread.Abort();

            thread = null;

            this.Dispose();
        }

        protected override void OnPause()
        {
            thread.Suspend();
        }

        protected override void OnContinue()
        {
            thread.Resume();
        }
    }
}
