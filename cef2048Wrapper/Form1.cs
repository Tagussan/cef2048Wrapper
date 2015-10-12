using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using CefSharp.Internals;
using System.IO.Ports;
using System.IO;
using System.Reflection;

namespace cef2048Wrapper
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser browser;
        private Boolean running;
        private SerialPort serial;
        private static String receivedData;
        private static readonly string _myPath = Application.StartupPath;
        private static readonly string _pagesPath = Path.Combine(_myPath, "pages");
        private string GetPagePath(string pageName)
        {
            return Path.Combine(_pagesPath, pageName);
        }
        public Form1()
        {
            InitializeComponent();
            initBrowser();
            var portList = SerialPort.GetPortNames();
            cmbPortName.Items.Clear();
            cmbPortName.Items.AddRange(portList);
            receivedData = "";
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void initSerial()
        {
            serial = new SerialPort(cmbPortName.Text);
            serial.BaudRate = 115200;
            serial.Parity = Parity.None;
            serial.StopBits = StopBits.One;
            serial.DataBits = 8;
            serial.Handshake = Handshake.None;
            serial.RtsEnable = false;
            serial.DataReceived += new SerialDataReceivedEventHandler(serialReceiveHandler);
            serial.Open();
        }

        private void initBrowser()
        {
            browser = new ChromiumWebBrowser(GetPagePath("index.html"))
            {
                Dock = DockStyle.Fill
            };
            pnlBrowser.Controls.Add(browser);
            browser.ConsoleMessage += new EventHandler<ConsoleMessageEventArgs>(consoleReceiveHandler);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (running)
            {
                //end process
                button1.Text = "Start";
                running = false;
            }
            else if (!running && cmbPortName.Text != "")
            {
                //start process
                button1.Text = "Stop";
                running = true;
                initSerial();

            }
        }

        private void serialReceiveHandler(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string indata = sp.ReadExisting();
            receivedData += indata;
        }

        private void consoleReceiveHandler(object sender, ConsoleMessageEventArgs e)
        {
            this.Invoke((Action<object>)((obj) =>
            {
                browserConsole.AppendText(e.Message + "\n");
            }), new Object());
        }

    }
}
    
