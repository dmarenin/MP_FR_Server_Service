using System;
using System.IO.Ports;
using System.Text;
using System.Threading;

//created marenin dl 01042018

namespace MP_FR_Command
{
    public static class Command
    {
        public static string Exec(string Data, string ConnectPort, string ConnectBaudrate, string ReadTimeout, License lic)
        {
            string var_a = "</ArmResponse>";

            string result = "";

            lic.ModifiedData(Data);

            if (String.IsNullOrEmpty(Data))
            {
                return result;
            }

            SerialPort serPort = new SerialPort(ConnectPort, Convert.ToInt32(ConnectBaudrate), Parity.None, 8, StopBits.One);

            if (serPort.IsOpen)
            {
                Log.Write("port is open");

                return result;
            }

            serPort.Encoding = Encoding.UTF8;

            serPort.ReadBufferSize = 8192 * 4;
            serPort.WriteBufferSize = 8192 * 4;

            try
            {
                serPort.Open();
            }
            catch
            {
                Log.Write("error port open");

                serPort = null;
                return result;
            }

            Log.Write("write data start");

            serPort.WriteLine(Data);

            Log.Write("write data end");

            serPort.ReadTimeout = -1;

            if (!String.IsNullOrEmpty(ReadTimeout))
            {
                serPort.ReadTimeout = Convert.ToInt32(ReadTimeout);
            }

            Log.Write("read data start");

            try
            {
                //Thread.Sleep(25);

                //Log.Write("buffer read");

                result = serPort.ReadLine();

                //Thread.Sleep(25);

                if (!result.Contains(var_a))
                {
                    while (true)
                    {
                        result = result + serPort.ReadLine();
                        if (result.Contains(var_a))
                        {
                            break;
                        }
                    }
                }
            }
            catch
            {
                serPort.Close();
                serPort = null;

                Log.Write("read data error");

                return result;
            }

            Log.Write("read data end");

            serPort.Close();

            serPort = null;

            return result;
        }
    }
}