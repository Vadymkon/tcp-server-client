using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Lab4
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        Thread T;
        bool mustStop;

        protected override void OnStart(string[] args)
        {
            T = new Thread(WorkerThread);
            T.Start();
        }

        protected override void OnStop()
        {
            if ((T != null) && (T.IsAlive))
            {
                mustStop = true;
            }
        }

        void WorkerThread()
        {
            WriteLog("Service is started!");


            WqlEventQuery Q1 = new WqlEventQuery("select * from __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_IP4PersistedRouteTable'");
            ManagementEventWatcher W1 = new ManagementEventWatcher(Q1);
            W1.EventArrived += new EventArrivedEventHandler(DeletedLog);
            W1.Start();

            WqlEventQuery Q2 = new WqlEventQuery("select * from __InstanceCreationEvent WITHIN 1  WHERE TargetInstance ISA 'Win32_IP4PersistedRouteTable'");
            ManagementEventWatcher W2 = new ManagementEventWatcher(Q2);
            W2.EventArrived += new EventArrivedEventHandler(AddedLog);
            W2.Start();

            /*
             PART 2
            TCP Server XML
             */


            while (!mustStop)
            {
                IPAddress IP = IPAddress.Parse("192.168.144.132");
                IPEndPoint ipEndPoint = new IPEndPoint(IP, 45000);
                Socket S = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    S.Bind(ipEndPoint);
                    S.Listen(10);

                    while (true)
                    {
                        using (Socket H = S.Accept())
                        {
                            //GET
                            IPEndPoint L = new IPEndPoint(IP, 0);
                            EndPoint R = (EndPoint)(L);
                            byte[] D = new byte[10000];
                            int Receive = H.ReceiveFrom(D, ref R);
                            string Request = Encoding.GetEncoding(1251).GetString(D, 0, Receive);
                            int num = 1; 
                            if (Request.StartsWith("<Request2>")) num = 2; //knowledge about mode
                            
                            if (num == 1)
                            {
                                if (File.Exists(@"C:\work\Request-1.XML")) File.Delete(@"C:\work\Request-1.XML");
                                if (File.Exists(@"C:\work\Response-1.XML")) File.Delete(@"C:\work\Response-1.XML");
                            }
                            if (num == 2)
                            {
                                if (File.Exists(@"C:\work\Request-2.XML")) File.Delete(@"C:\work\Request-2.XML");
                                if (File.Exists(@"C:\work\Response-2.XML")) File.Delete(@"C:\work\Response-2.XML");
                            }
                            
                            WriteLog(Request, $"Request-{num}.XML",false);

                            //have value from XML-1-client
                            XmlDocument doc = new XmlDocument();
                            doc.Load($@"C:\work\Request-{num}.XML");
                            string pathToXML = doc.GetElementsByTagName("Text")[0].InnerText;

                            //WMI staff
                            if (num == 1)
                            {
                            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ExecutablePath LIKE '" + pathToXML.Replace(@"\", @"\\") + "%'");
                            ManagementObjectCollection collection = searcher.Get();
                            WriteLog($"made Query --- {"SELECT * FROM Win32_Process WHERE ExecutablePath LIKE '" + pathToXML.Replace(@"\", @"\\") + "%'"}");

                            XmlDocument responseDoc = new XmlDocument();
                            XmlElement root = responseDoc.CreateElement("Response");
                            responseDoc.AppendChild(root);
                            foreach (ManagementObject process in collection)
                            {
                                XmlElement processElement = responseDoc.CreateElement("Process");

                                processElement.SetAttribute("Description", process["Description"].ToString());
                                processElement.SetAttribute("ExecutablePath", process["ExecutablePath"].ToString());
                                processElement.SetAttribute("KernelModeTime", process["KernelModeTime"].ToString());

                                root.AppendChild(processElement);
                            }
                            responseDoc.Save(@"C:\work\Response-1.xml");
                            }
                            else if (num == 2)
                            {

                                try
                                {
                                    /*  WriteLog(pathToXML);
                                  ManagementClass P = new ManagementClass("Win32_Process");
                                       object[] IN = { pathToXML, null, null, 0 };
                                      object R1 = P.InvokeMethod("Terminate", IN);*/

                                    bool isProcessIn = false;
                                    foreach (Process Proc in Process.GetProcesses())
                                        if (Proc.ProcessName.Equals(pathToXML.Replace(".exe", "")))  //Process Excel?
                                        {
                                            Proc.Kill();
                                            isProcessIn = true;
                                        } 
                                    WriteLog(isProcessIn ? $"Process {pathToXML} was killed." : $"Wasn't process like {pathToXML} . ", $"Response-{num}.XML");
                                }
                                catch (Exception e)
                                {
                                    WriteLog(e.Message, $"Response-{num}.XML");
                                }
                            }

                            //ANSWER
                            string W = File.ReadAllText($@"C:\work\Response-{num}.XML", Encoding.GetEncoding(1251));
                            byte[] M = Encoding.GetEncoding(1251).GetBytes(W);
                            H.Send(M);

                            H.Shutdown(SocketShutdown.Both);

                        }
                    }
                }
                catch (Exception e)
                {
                    WriteLog(e.Message);
                }
            }
        }

        static void DeletedLog(object source, EventArrivedEventArgs e)
        {
            WriteLog($"DELETED ROUTE");
        }
        static void AddedLog(object source, EventArrivedEventArgs e)
        {
            WriteLog($"Added ROUTE");
        }

        private static void WriteLog(string z, string filename = "Laba-4.log", bool isLog = true)
        {
            using (StreamWriter F = new StreamWriter($@"C:\work\{filename}", true))
            {
                if (isLog) F.WriteLine(DateTime.Now + " " + z);
                else       F.WriteLine(z);
            }
        }

        void Cmd(string line)
        {
            Process.Start(new ProcessStartInfo { FileName = "cmd", Arguments = $"/c {line}", WindowStyle = ProcessWindowStyle.Hidden });
        }
    }
}
