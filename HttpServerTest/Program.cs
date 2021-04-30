using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Linq;

namespace HttpServerTest {
    class Program {
        public static string SendResponse(HttpListenerRequest request) {
            //return string.Format("<HTML><BODY>My web page.<br>{0}</BODY></HTML>", DateTime.Now);
            Console.WriteLine("[{0}] {1}", DateTime.Now.ToString("yyyy/dd/MM HH:mm:ss"), request.Url.OriginalString);

            string r = "";
            foreach (string key in request.QueryString.AllKeys)
                r += string.Format("{0}={1}{2}", key, request.QueryString[key], request.QueryString.AllKeys.ToList().IndexOf(key) != request.QueryString.AllKeys.Length - 1 ? "\n" : "");
            Console.WriteLine(r);

            ParseArgs(r);

            //System.Diagnostics.Process.Start("http://google.com");

            return string.Format("Test web server: {0}", DateTime.Now.ToString("yyyy/dd/MM HH:mm:ss"));
        }

        public static void ParseArgs(string args) {
            string s = args.Replace("\r\n", "|").Split('|')[0];
            string key = s.Split('=')[0];
            string value = s.Split('=')[1];

            if (key == "browser") {
                System.Diagnostics.Process.Start("http://" + value);
            }
        }

        public static object GetComportNames() {
            return new string[] { "COM1", "COM2", "COM3" };
        }

        static void Main(string[] args) {
            //var ws = new WebServer(SendResponse, "http://localhost:8080/test/");
            var ws = new WebServer(SendResponse, "http://127.0.0.1:8080/");
            //var ws = new WebServer(SendResponse, "http://192.168.1.17/");
            ws.Run();
            Console.WriteLine("Press a key to quit.");
            Console.ReadKey();
            ws.Stop();
        }
    }

    class WebServer {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(IReadOnlyCollection<string> prefixes, Func<HttpListenerRequest, string> method) {
            if (!HttpListener.IsSupported) {
                throw new NotSupportedException("Needs Windows XP SP2, Server 2003 or later.");
            }

            // URI prefixes are required eg: "http://localhost:8080/test/"
            if (prefixes == null || prefixes.Count == 0) {
                throw new ArgumentException("URI prefixes are required");
            }

            if (method == null) {
                throw new ArgumentException("responder method required");
            }

            foreach (var s in prefixes) {
                _listener.Prefixes.Add(s);
            }

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
           : this(prefixes, method) {
        }

        public void Run() {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Console.WriteLine("Webserver running...");
                try {
                    while (_listener.IsListening) {
                        ThreadPool.QueueUserWorkItem(c =>
                        {
                            var ctx = c as HttpListenerContext;
                            try {
                                if (ctx == null) {
                                    return;
                                }

                                var rstr = _responderMethod(ctx.Request);
                                var buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            } catch {
                                // ignored
                            } finally {
                                // always close the stream
                                if (ctx != null) {
                                    ctx.Response.OutputStream.Close();
                                }
                            }
                        }, _listener.GetContext());
                    }
                } catch (Exception ex) {
                    // ignored
                }
            });
        }

        public void Stop() {
            _listener.Stop();
            _listener.Close();
        }
    }
}
