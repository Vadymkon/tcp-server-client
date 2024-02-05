using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace TCP_Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        async void button1_Click(object sender, EventArgs e)
        {
            //delete preconfig
            if (File.Exists("Request-1.xml")) File.Delete("Request-1.xml");
            if (File.Exists("Response-1.xml")) File.Delete("Response-1.xml");
            if (File.Exists("Response-1-sorted.xml")) File.Delete("Response-1-sorted.xml");

            //create file
            using (StreamWriter writer = new StreamWriter("Request-1.xml"))
            {
                writer.WriteLine("<Request>");
                writer.WriteLine("<Text>" + textBox2.Text + "</Text>");
                writer.WriteLine("</Request>");
            }

            WriteLog(Speaking(@"C:\work\Request-1.xml"), "Response-1.XML");

            //sort
            List<string> Lines = File.ReadAllLines(@"C:\work\Response-1.XML").ToList().Where(x => x.Contains("Process")).ToList().OrderBy(x => Convert.ToInt32(x.Substring(x.IndexOf("KernelModeTime=") + 16,
            x.Length - (x.IndexOf("KernelModeTime=") + 16) - (x.Length - x.LastIndexOf("\""))))
            ).Reverse().ToList() ;
            /*
            Lines.ForEach(x => WriteLog(x.Substring(x.IndexOf("KernelModeTime=") + 16,
                x.Length - (x.IndexOf("KernelModeTime=") + 16) - (x.Length - x.LastIndexOf("\"")))));
             */
            //create file
            using (StreamWriter writer = new StreamWriter("Response-1-sorted.xml"))
            {
                writer.WriteLine("<Response>");
                Lines.ForEach(x => writer.WriteLine(x));
                writer.WriteLine("</Response>");
                writer.WriteLine("<Response />");
            }
            comboBox1.Items.Clear();
            Lines.ForEach(x => comboBox1.Items.Add(x.Substring(x.IndexOf("Description=\"")+13, 
                 x.IndexOf(" ExecutablePath")- x.IndexOf("Description=\"") - 14 )));
            if (comboBox1.Items.Count != 0) button2.Enabled = true;
        }
        
        protected string Answer;

        //отправляет файл
        public string Speaking(string XmlPath)
        {
            string F = File.ReadAllText(XmlPath, Encoding.GetEncoding(1251));
            byte[] M = Encoding.GetEncoding(1251).GetBytes(F);
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(textBox1.Text), 45000);
            byte[] bytes = new byte[1000000];
            using (Socket S = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                S.Connect(ipEndPoint);
                S.Send(M);
                int bytesRec = S.Receive(bytes);
                Answer = Encoding.GetEncoding(1251).GetString(bytes, 0, bytesRec);
                return Answer;
                S.Shutdown(SocketShutdown.Both);
            }
        }
         

        private static void WriteLog(string z, string filename = "Laba-4.log")
        {
            using (StreamWriter F = new StreamWriter($@"C:\work\{filename}", true))
            {
                //F.WriteLine(DateTime.Now + " " + z);
                F.WriteLine(z);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (File.Exists("Request-2.xml")) File.Delete("Request-2.xml");
            if (File.Exists("Response-2.xml")) File.Delete("Response-2.xml");
            //create file
            using (StreamWriter writer = new StreamWriter("Request-2.xml"))
            {
                writer.WriteLine("<Request2>");
                writer.WriteLine("<Text>" + comboBox1.Text + "</Text>");
                writer.WriteLine("</Request2>");
            }

            WriteLog(Speaking(@"C:\work\Request-2.xml"), "Response-2.XML");

            label1.Text = File.ReadAllText(@"C:\work\Response-2.XML");
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
