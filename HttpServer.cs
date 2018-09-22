using MP_FR_Command;
using System;
using System.Collections;
using System.IO;
using System.Net.Sockets;
using System.Threading;

// offered to the public domain for any use with no restriction
// and also with no warranty of any kind, please enjoy. - David Jeske. 

// simple HTTP explanation
// http://www.jmarshall.com/easy/http/

//modifed marenin dl 01042018

namespace Bend.Util
{
    public class HttpProcessor
    {
        public TcpClient socket;
        public HttpServer srv;

        private Stream inputStream;
        public StreamWriter outputStream;

        public String http_method;
        public String http_url;
        public String http_protocol_versionstring;
        public Hashtable httpHeaders = new Hashtable();


        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv)
        {
            this.socket = s;
            this.srv = srv;
        }


        private string streamReadLine(Stream inputStream)
        {
            int next_char;
            string data = "";
            while (true)
            {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }
            return data;
        }
        public void process()
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            inputStream = new BufferedStream(socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try
            {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET"))
                {
                    handleGETRequest();
                }
                else if (http_method.Equals("POST"))
                {
                    handlePOSTRequest();
                }
            }
            catch (Exception e)
            {
                Log.Write("Exception: " + e.ToString());

                writeFailure();
            }
            try
            {
                outputStream.Flush();
            }
            catch (Exception e)
            {
                Log.Write("Exception: " + e.ToString());
            }

            // bs.Flush(); // flush any remaining output
            inputStream = null; outputStream = null; // bs = null;            
            socket.Close();
        }

        public void parseRequest()
        {
            String request = streamReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            Log.Write("starting: " + request);
        }

        public void readHeaders()
        {
            Log.Write("read headers");

            String line;
            while ((line = streamReadLine(inputStream)) != null)
            {
                if (line.Equals(""))
                {
                    Log.Write("got headers");
                    return;
                }

                int separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' '))
                {
                    pos++; // strip any spaces
                }

                string value = line.Substring(pos, line.Length - pos);

                Log.Write("header: " + name + value);

                httpHeaders[name] = value;
            }
        }

        public void handleGETRequest()
        {
            srv.handleGETRequest(this);
        }

        private const int BUF_SIZE = 4096;
        public void handlePOSTRequest()
        {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            Log.Write("get post data start");

            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length"))
            {
                content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                if (content_len > MAX_POST_SIZE)
                {
                    throw new Exception(
                        String.Format("POST Content-Length({0}) too big for this simple server",
                          content_len));
                }
                byte[] buf = new byte[BUF_SIZE];
                int to_read = content_len;
                while (to_read > 0)
                {
                    Log.Write("starting Read, to_read=" + to_read);

                    int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));

                    Log.Write("read finished, numread = " + numread);

                    if (numread == 0)
                    {
                        if (to_read == 0)
                        {
                            break;
                        }
                        else {
                            throw new Exception("client disconnected during post");
                        }
                    }
                    to_read -= numread;
                    ms.Write(buf, 0, numread);
                }
                ms.Seek(0, SeekOrigin.Begin);
            }

            Log.Write("get post data end");

            srv.handlePOSTRequest(this, new StreamReader(ms));
        }

        public void writeSuccess()
        {
            outputStream.Write("HTTP/1.0 200 OK\n");
            outputStream.Write("Content-Type: text/html\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");

            Log.Write("close HTTP/1.0 200 OK");
        }

        public void writeFailure()
        {
            outputStream.Write("HTTP/1.0 404 File not found\n");
            outputStream.Write("Connection: close\n");
            outputStream.Write("\n");

            Log.Write("close HTTP/1.0 404 File not found");
        }
    }

    public abstract class HttpServer
    {

        protected int port;
        TcpListener listener;
        bool is_active = true;

        public HttpServer(int port)
        {
            this.port = port;
        }

        public void listen()
        {
            Log.Write("start listen at " + port.ToString());

            listener = new TcpListener(port);
            listener.Start();
            while (is_active)
            {
                TcpClient s = listener.AcceptTcpClient();

                Log.Write("accept " + s.Client.RemoteEndPoint.ToString());

                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public abstract void handleGETRequest(HttpProcessor p);
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
    }

    public class MyHttpServer : HttpServer
    {
        License license = null;

        public MyHttpServer(int port)
            : base(port)
        {
            license = new License();

        }
        public override void handleGETRequest(HttpProcessor p)
        {

            Log.Write("request: " + p.http_url);

            string result = "";

            if (p.http_url == "/GetVolumeSerialNumber")
            {
                var license = ((Bend.Util.MyHttpServer)p.srv).license;

                result = license.GetVolumeSerialNumber();
            }
            else if (p.http_url == "/GetAd")
            {
                var license = ((Bend.Util.MyHttpServer)p.srv).license;

                foreach (var a in license.ads)
                {
                    result += a;
                }
            }
    
            p.writeSuccess();
            p.outputStream.WriteLine(result);
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData)
        {
            var license = ((Bend.Util.MyHttpServer)p.srv).license;

            Log.Write("request: " + p.http_url);

            string data = inputData.ReadToEnd();

            Log.Write("data: " + data);

            string connectPort = "COM1";
            if (p.httpHeaders["Port"] != null)
            {
                connectPort = p.httpHeaders["Port"].ToString();
            }

            string connectBaudrate = "115200";
            if (p.httpHeaders["Baudrate"] != null)
            {
                connectBaudrate = p.httpHeaders["Baudrate"].ToString();
            }

            string readTimeout = "";
            if (p.httpHeaders["ReadTimeout"] == null)
            {
                readTimeout = p.httpHeaders["ReadTimeout"].ToString();
            }

            string result = Command.Exec(data, connectPort, connectBaudrate, readTimeout, license);

            Log.Write("result: " + result);

            p.writeSuccess();
            p.outputStream.WriteLine(result);
        }
    }
}