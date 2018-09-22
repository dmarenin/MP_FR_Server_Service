using Bend.Util;
using System;
using System.Threading;

//craeted marenin dl 01042018

namespace MP_FR_Command
{
    public static class Starter
    {
        public static int Main(String[] args)
        {
            HttpServer httpServer;
            if (args.GetLength(0) > 0)
            {
                httpServer = new MyHttpServer(Convert.ToInt16(args[0]));
            }
            else {
                httpServer = new MyHttpServer(13000);
            }
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();
            return 0;
        }
    }
}