#pragma warning disable CS4014

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Globalization;

namespace ConsoleApplication1
{
    class Program
    {
        const int remotePort = 8888;
        const int localPort = 7000;
        const int localPortRequest = 6000;

        const double ku = 0.306;
        const double ki = 0.0245;

        static List<lamp> lamps = new List<lamp>();

        private static byte[] getDataRequest = { 1, 00, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static byte[] LampONRequest = { 1, 160, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static byte[] LampOFFRequest = { 1, 80, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        enum logType{ DataLog, OnOffLog, ManualOnOffLog, SunsetsLog, ErrorsLog };

        static void logger(string str, logType type)
        {
            try
            {
                switch (type)
                {
                    case logType.SunsetsLog:
                        //File.AppendAllText(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\sunsets.log", DateTime.Now.ToString() + ": " + str + "\r\n");
                        break;
                    case logType.ErrorsLog:
                        Console.WriteLine(DateTime.Now.ToString() + ": " + str + "\r\n");
                        //File.AppendAllText(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\errors.log", DateTime.Now.ToString() + ": " + str + "\r\n");
                        break;
                }
            }
            catch (Exception) { }
        }
        static void logger(string str,double napr, double tok, logType type, int id)
        {
            try
            {
                switch (type)
                {
                    case logType.DataLog:
                        if (!Directory.Exists(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\DataLogs\" + id.ToString())) Directory.CreateDirectory(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\DataLogs\" + id.ToString());
                        //File.AppendAllText(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\DataLogs\" + id.ToString() + @"\" + DateTime.Now.ToShortDateString() + ".log", str + "\r\n");
                        var culture = CultureInfo.CreateSpecificCulture("en-CA");
                        sqlExec("INSERT INTO valuesofconsumption(consumper_id, capasitor_id, capasity,voltage,amperage) VALUES(" + id.ToString("F", culture) + ", 1, " + (tok * napr).ToString("F", culture) + ", " + napr.ToString("F", culture) + ", " + tok.ToString("F", culture) + "); ");
                        break;
                }

            }
            catch (Exception) { }
        }
        static void logger(bool str, logType type, int id)
        {
            try
            {
                switch (type)
                {
                    case logType.OnOffLog:
                        if (!Directory.Exists(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\OnOffLogs")) Directory.CreateDirectory(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\OnOffLogs");
                        //File.AppendAllText(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\OnOffLogs\" + DateTime.Now.ToShortDateString() + ".log", "Лампа с " + id.ToString() + " была " + (!str ? "выключена" : "включена") + " в " + DateTime.Now.ToShortTimeString() + "\r\n");
                        break;
                }
            }
            catch (Exception) { }
        }
        static void logger(bool str, logType type, int id, string ip)
        {
            try
            {
                switch (type)
                {
                    case logType.ManualOnOffLog:
                        if (!Directory.Exists(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\ManualOnOffLogs")) Directory.CreateDirectory(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\ManualOnOffLogs");
                        //File.AppendAllText(@"C:\Users\ArduinoAdmin\Desktop\Lamps\logs\ManualOnOffLogs\" + DateTime.Now.ToShortDateString() + ".log", "Лампа с " + id.ToString() + " была " + (!str ? "выключена" : "включена") + " в " + DateTime.Now.ToShortTimeString() + " пользователем с ip " + ip + "\r\n");
                        break;
                }
            }
            catch (Exception) { }
        }
        class Point
        {
            public int x { get; set; }
            public int y { get; set; }
        }
        class lamp
        {
            public bool connection
            {
                get; set;
            } = false;
            public bool enable
            {
                get; set;
            } = false;
            
            public int id
            {
                get; set;
            }
            public string status
            {
                get; set;
            }
            public string name
            {
                get; set;
            }
            public string priznak
            {
                get; set;
            }
            public string mac
            {
                get; set;
            }
            public string ip
            {
                get; set;
            }
            public string mask
            {
                get; set;
            }
            public string gateway
            {
                get; set;
            }
            public string type
            {
                get; set;
            }
            public int line
            {
                get; set;
            }
            public Point point
            {
                get; set;
            } = new Point();
            public void lampOff()
            {
                try
                {
                    byte[] rtrn = new byte[30];

                    Ping pingSender = new Ping();
                    PingOptions options = new PingOptions();
                    options.DontFragment = true;
                    byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    int timeout = 200;
                    PingReply reply = pingSender.Send("10.200.120." + id.ToString(), timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        UdpClient udp = new UdpClient(localPortRequest + 100 + id);
                        Send(LampOFFRequest, "10.200.120." + id.ToString(), localPortRequest, ref udp);
                        IPEndPoint RemoteIpEndPoint = null;
                        while (udp.Available == 0) { Thread.Sleep(500); }
                        if (udp.Available != 0)
                        {
                            rtrn = udp.Receive(ref RemoteIpEndPoint);
                            lamp remoteLamp = lamps.Find(x => x.id == rtrn[0]);
                            remoteLamp.connection = true;
                            if (rtrn[29] == 0) remoteLamp.enable = true;
                            else remoteLamp.enable = false;
                            remoteLamp.name = "Номер: " + rtrn[0];
                            remoteLamp.priznak = "Признак: " + rtrn[1];
                            remoteLamp.mac = "MAC: " + rtrn[2].ToString("X") + ":" + rtrn[3].ToString("X") + ":" + rtrn[4].ToString("X") + ":" + rtrn[5].ToString("X") + ":" + rtrn[6].ToString("X") + ":" + rtrn[7].ToString("X");
                            remoteLamp.ip = "IP: " + rtrn[8].ToString() + "." + rtrn[9].ToString() + "." + rtrn[10].ToString() + "." + rtrn[11].ToString();
                            remoteLamp.mask = "Маска: " + rtrn[12].ToString() + "." + rtrn[13].ToString() + "." + rtrn[14].ToString() + "." + rtrn[15].ToString();
                            remoteLamp.gateway = "Шлюз: " + rtrn[16].ToString() + "." + rtrn[17].ToString() + "." + rtrn[18].ToString() + "." + rtrn[19].ToString();
                        }
                        logger(false, logType.OnOffLog, id);
                        udp.Close();
                    }
                }
                catch (Exception ex)
                {
                    logger("Ошибка в lampOff" + ex.ToString(), logType.ErrorsLog);
                }
            }
            public void lampOn()
            {
                UdpClient udp = new UdpClient(localPortRequest + 100 + id);
                try
                {
                    byte[] rtrn = new byte[30];

                    Ping pingSender = new Ping();
                    PingOptions options = new PingOptions();
                    options.DontFragment = true;
                    byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    int timeout = 200;
                    PingReply reply = pingSender.Send("10.200.120." + id.ToString(), timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        Send(LampONRequest, "10.200.120." + id.ToString(), localPortRequest, ref udp);
                        IPEndPoint RemoteIpEndPoint = null;
                        while (udp.Available == 0) { Thread.Sleep(500); }
                        if (udp.Available != 0)
                        {
                            rtrn = udp.Receive(ref RemoteIpEndPoint);
                            lamp remoteLamp = lamps.Find(x => x.id == rtrn[0]);
                            remoteLamp.connection = true;
                            if (rtrn[29] == 0) remoteLamp.enable = true;
                            else remoteLamp.enable = false;
                            remoteLamp.name = "Номер: " + rtrn[0];
                            remoteLamp.priznak = "Признак: " + rtrn[1];
                            remoteLamp.mac = "MAC: " + rtrn[2].ToString("X") + ":" + rtrn[3].ToString("X") + ":" + rtrn[4].ToString("X") + ":" + rtrn[5].ToString("X") + ":" + rtrn[6].ToString("X") + ":" + rtrn[7].ToString("X");
                            remoteLamp.ip = "IP: " + rtrn[8].ToString() + "." + rtrn[9].ToString() + "." + rtrn[10].ToString() + "." + rtrn[11].ToString();
                            remoteLamp.mask = "Маска: " + rtrn[12].ToString() + "." + rtrn[13].ToString() + "." + rtrn[14].ToString() + "." + rtrn[15].ToString();
                            remoteLamp.gateway = "Шлюз: " + rtrn[16].ToString() + "." + rtrn[17].ToString() + "." + rtrn[18].ToString() + "." + rtrn[19].ToString();

                        }
                        logger(true, logType.OnOffLog, id);
                        udp.Close();
                    }
                    else
                    {
                        connection = false;
                        status = "Состояние: не отвечает";
                        udp.Close();
                    }
                }
                catch (Exception ex)
                {
                    udp.Close();
                    logger("Ошибка в lampOn" + ex.ToString(), logType.ErrorsLog);
                }
                finally
                {
                    udp.Close();
                }
            }
            public int countGetsDatas = 0;
            public bool getData(ref UdpClient UdpClient)
            {
                try
                {
                    countGetsDatas++;
                    Ping pingSender = new Ping();
                    PingOptions options = new PingOptions();
                    options.DontFragment = true;
                    byte[] buffer = Encoding.ASCII.GetBytes("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
                    int timeout = 2000;
                    PingReply reply = pingSender.Send("10.200.120." + id.ToString(), timeout, buffer, options);
                    if (reply.Status == IPStatus.Success)
                    {
                        Send(getDataRequest, "10.200.120." + id, localPort, ref UdpClient);
                        return true;
                    }
                    else
                    {
                        connection = false;
                        status = "Состояние: не отвечает";
                        return false;
                    }
                }
                catch(Exception ex)
                {
                    logger("Ошибка в getData" + ex.ToString(), logType.ErrorsLog);
                    return false;
                }
            }
            private void Send(byte[] datagram, string ip, int port, ref UdpClient sender)
            {
                sender.Client.SendTimeout = 500;
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), remotePort);
                try
                {
                    sender.Send(datagram, datagram.Length, endPoint);
                }
                catch (Exception ex)
                {
                    logger("Ошибка в Send" + ex.ToString(), logType.ErrorsLog);
                }
            }
        }


        static void WebServer()
        {
            while (true)
            {
                try
                {
                    HttpListener server = new HttpListener();
                    server.Prefixes.Add("http://+:8080/");
                    try
                    {
                        server.Start();
                    }
                    catch (Exception ex)
                    {
                        logger("Ошибка в webserver start" + ex.ToString(), logType.ErrorsLog);
                    }
                    while (true)
                    {
                        try
                        {
                            HttpListenerContext context = server.GetContext();
                            HttpListenerResponse response = context.Response;

                            if (context.Request.HttpMethod == "GET")
                            {
                                string page = "index.html";
                                TextReader tr = new StreamReader(page);
                                string msg = tr.ReadToEnd();
                                byte[] buffer = Encoding.UTF8.GetBytes(msg);
                                tr.Close();
                                response.ContentLength64 = buffer.Length;
                                try
                                {
                                    Stream st = response.OutputStream;
                                    st.Write(buffer, 0, buffer.Length);
                                }
                                catch (Exception ex)
                                {
                                    logger("Ошибка в GET responsing: " + ex.ToString(), logType.ErrorsLog);
                                }
                                finally
                                {
                                    context.Response.Close();
                                }
                            }
                            if (context.Request.HttpMethod == "POST")
                            {
                                try
                                {
                                    context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
                                    Stream input = context.Request.InputStream;
                                    Stream output = context.Response.OutputStream;
                                    byte[] bytes = new byte[(int)context.Request.ContentLength64];
                                    input.Read(bytes, 0, (int)context.Request.ContentLength64);
                                    input.Close();
                                    byte[] responsebuffer;
                                    string result = Encoding.UTF8.GetString(bytes);
                                    if (result == "getInfo")
                                    {
                                        responsebuffer = LampsDataToJSON();
                                        output.Write(responsebuffer, 0, responsebuffer.Length);
                                    }
                                    if (result.Substring(0, 6) == "lampOn")
                                    {
                                        lamps.Find(x => x.id == int.Parse(result.Substring(7))).lampOn();
                                        Thread.Sleep(100);
                                        responsebuffer = LampsDataToJSON();
                                        output.Write(responsebuffer, 0, responsebuffer.Length);
                                        logger(true, logType.ManualOnOffLog, int.Parse(result.Substring(7)), context.Request.RemoteEndPoint.Address.ToString()); Console.WriteLine(DateTime.Now.ToString() + ": лампа с id " + result.Substring(7) + " включена пользователем с ip " + context.Request.RemoteEndPoint.Address);
                                    }
                                    if (result.Substring(0, 7) == "lampOff")
                                    {
                                        lamps.Find(x => x.id == int.Parse(result.Substring(7))).lampOff();
                                        Thread.Sleep(100);
                                        responsebuffer = LampsDataToJSON();
                                        output.Write(responsebuffer, 0, responsebuffer.Length);
                                        logger(false, logType.ManualOnOffLog, int.Parse(result.Substring(7)), context.Request.RemoteEndPoint.Address.ToString()); Console.WriteLine(DateTime.Now.ToString() + ": лампа с id " + result.Substring(7) + " выключена пользователем с ip " + context.Request.RemoteEndPoint.Address);
                                    }
                                    try
                                    {
                                        output.Close();
                                    }
                                    catch (Exception)
                                    {
                                        output = null;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    logger("Ошибка в POST responsing: " + ex.ToString(), logType.ErrorsLog);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger("Ошибка в webserver: " + ex.ToString(), logType.ErrorsLog);
                        }
                    }
                }
                catch (Exception) { }
            }
        }
        static void LampSender(object lampId)
        {
            while (true)
            {
                try
                {
                    lamp lamp = lamps.Find(x => x.id == (int)lampId);
                    UdpClient receivingUdpClient;
                    IPEndPoint RemoteIpEndPoint = null;
                    byte[] rtrn;
                    using (receivingUdpClient = new UdpClient(localPort + lamp.id))
                    {
                        while (true)
                        {
                            Thread.Sleep(4000);
                            if (lamp.getData(ref receivingUdpClient))
                            {
                                int i = 0;
                                while (receivingUdpClient.Available == 0) { i++; if (i > 5) break; Thread.Sleep(30); }
                                if (receivingUdpClient.Available != 0)
                                {
                                    rtrn = receivingUdpClient.Receive(ref RemoteIpEndPoint);
                                    lamp remoteLamp = lamps.Find(x => x.id == rtrn[0]);
                                    remoteLamp.connection = true;
                                    if (rtrn[29] == 0) remoteLamp.enable = true;
                                    else remoteLamp.enable = false;
                                    remoteLamp.name = "Номер: " + rtrn[0];
                                    remoteLamp.priznak = "Признак: " + rtrn[1];
                                    remoteLamp.mac = "MAC: " + rtrn[2].ToString("X") + ":" + rtrn[3].ToString("X") + ":" + rtrn[4].ToString("X") + ":" + rtrn[5].ToString("X") + ":" + rtrn[6].ToString("X") + ":" + rtrn[7].ToString("X");
                                    remoteLamp.ip = "IP: " + rtrn[8].ToString() + "." + rtrn[9].ToString() + "." + rtrn[10].ToString() + "." + rtrn[11].ToString();
                                    remoteLamp.mask = "Маска: " + rtrn[12].ToString() + "." + rtrn[13].ToString() + "." + rtrn[14].ToString() + "." + rtrn[15].ToString();
                                    remoteLamp.gateway = "Шлюз: " + rtrn[16].ToString() + "." + rtrn[17].ToString() + "." + rtrn[18].ToString() + "." + rtrn[19].ToString();
                                    int highByte = rtrn[28] << 8;
                                    int lowByte = rtrn[27] & 0x00ff;
                                    int tok = highByte | lowByte;
                                    highByte = rtrn[20] << 8;
                                    lowByte = rtrn[21] & 0x00ff;
                                    int napr = highByte | lowByte;
                                    double fTok;
                                    double fNapr = ku * napr;
                                    if (tok - 512 < 0)
                                    {
                                        fTok = (512 - tok) * ki;
                                    }
                                    else
                                    {
                                        fTok = (tok - 512) * ki;
                                    }
                                    logger(DateTime.Now.ToShortTimeString() + " " + fTok.ToString() + " " + fNapr.ToString(),fNapr,fTok, logType.DataLog, remoteLamp.id);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger("Ошибка в lampSender: "+ex.ToString(), logType.ErrorsLog);
                }
            }
        }
        static void lampsGetDataAllSend()
        {
            foreach(lamp lmp in lamps)
            {
                Thread myThread = new Thread(new ParameterizedThreadStart(LampSender));
                myThread.Start(lmp.id);
            }
        }
        static void timeElapser()
        {
            while (true)
            {
                try
                {
                    HttpWebRequest req;
                    HttpWebResponse resp;
                    StreamReader sr;
                    string content;

                    req = (HttpWebRequest)WebRequest.Create("https://time.is/Dubna#time_zone");
                    resp = (HttpWebResponse)req.GetResponse();
                    sr = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                    content = sr.ReadToEnd();
                    sr.Close();

                    DateTime sunRise = DateTime.Parse(content.Substring(content.IndexOf("<li>Восход: ") + 12, 5));
                    DateTime sunSet = DateTime.Parse(content.Substring(content.IndexOf("<li>Закат: ") + 11, 5));
                    while (true)
                    {
                        Thread.Sleep(10000);
                        if (DateTime.Now.Hour == sunRise.Hour && DateTime.Now.Minute == sunRise.Minute)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + ": выключение ламп на рассвете");
                            req = (HttpWebRequest)WebRequest.Create("https://time.is/Dubna#time_zone");
                            resp = (HttpWebResponse)req.GetResponse();
                            sr = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                            content = sr.ReadToEnd();
                            sr.Close();

                            sunRise = DateTime.Parse(content.Substring(content.IndexOf("<li>Восход: ") + 12, 5));
                            sunSet = DateTime.Parse(content.Substring(content.IndexOf("<li>Закат: ") + 11, 5));
                            foreach (lamp lamp in lamps)
                            {
                                if (lamp.type == "\"lamp\"")
                                {
                                    lamp.lampOff();
                                }
                            }
                            Console.WriteLine(DateTime.Now + " - восход солнца");
                            logger("Восход солнца - выключение ламп", logType.SunsetsLog);
                        }
                        if (DateTime.Now.Hour == sunSet.Hour && DateTime.Now.Minute == sunSet.Minute)
                        {
                            Console.WriteLine(DateTime.Now.ToString() + ": включение ламп на закате");
                            req = (HttpWebRequest)WebRequest.Create("https://time.is/Dubna#time_zone");
                            resp = (HttpWebResponse)req.GetResponse();
                            sr = new StreamReader(resp.GetResponseStream(), Encoding.GetEncoding("utf-8"));
                            content = sr.ReadToEnd();
                            sr.Close();

                            sunRise = DateTime.Parse(content.Substring(content.IndexOf("<li>Восход: ") + 12, 5));
                            sunSet = DateTime.Parse(content.Substring(content.IndexOf("<li>Закат: ") + 11, 5));
                            foreach (lamp lamp in lamps)
                            {
                                if (lamp.type == "\"lamp\"")
                                {
                                    lamp.lampOn();
                                }
                            }
                            Console.WriteLine(DateTime.Now + " - заход солнца");
                            logger("Заход солнца - включение ламп", logType.SunsetsLog);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger("TimeElapser error: " + ex.ToString(), logType.ErrorsLog);
                }
            }
        }
        private static async Task lampsGetDataAllSendAsync()
        {
            await Task.Factory.StartNew(lampsGetDataAllSend);
        }
        private static async Task timeElapserAsync()
        {
            await Task.Factory.StartNew(timeElapser);
        }
        private static async Task WebServerAsync()
        {
            await Task.Factory.StartNew(WebServer);
        }
        static void ReadFile()
        {
            string[] lines = File.ReadAllLines("basa2");
            int i = -1;
            foreach(string line in lines)
            {
                try
                {
                    if (line.Substring(line.Length - 5, 5) == ":lamp")
                    {
                        i++;
                        lamps.Add(new lamp());
                    }
                }
                catch (Exception) { }
                try
                {
                    if (line.Remove(2) == "id")
                    {
                        lamps[i].id = int.Parse(line.Remove(0, 3));
                    }
                }
                catch (Exception) { }
                try
                {
                    if (line.Remove(4) == "line")
                    {
                        lamps[i].line = int.Parse(line.Remove(0, 5));
                    }
                }
                catch (Exception) { }
                try
                {
                    if (line.Remove(4) == "type")
                    {
                        lamps[i].type = line.Remove(0, 5);
                    }
                }
                catch (Exception) { }
                try
                {
                    if (line.Remove(1) == "x")
                    {
                        lamps[i].point.x = int.Parse(line.Remove(0, 2));
                    }
                }
                catch (Exception) { }
                try
                {
                    if (line.Remove(1) == "y")
                    {
                        lamps[i].point.y = int.Parse(line.Remove(0, 2));
                    }
                }
                catch (Exception) { }

            }
        }
        static byte[] LampsDataToJSON()
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return Encoding.ASCII.GetBytes(serializer.Serialize(lamps));
        }
        static void sqlExec(string commandText)
        {
            try
            {
                string Connect = "Database=arduinodatabase;Data Source=10.230.0.161;User Id=root;Password=MySqLrOoT;";

                MySqlConnection myConnection = new MySqlConnection(Connect);
                MySqlCommand myCommand = new MySqlCommand(commandText, myConnection);

                myConnection.Open(); //Устанавливаем соединение с базой данных.

                myCommand.ExecuteNonQuery();

                myConnection.Close(); 

            }
            catch(Exception ex)
            {
                logger("Ошибка в sqlExec: "+ex.ToString(), logType.ErrorsLog);
            }
        }
        static void Main(string[] args)
        {
            ReadFile();
            timeElapserAsync();
            lampsGetDataAllSendAsync();
            WebServerAsync();
            Console.ReadKey();
        }
    }
}