using System;
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
using System.IO.Ports;

namespace cef2048Wrapper
{
    public partial class Form1 : Form
    {
        private ChromiumWebBrowser browser;
        public Form1()
        {
            InitializeComponent();
            browser = new ChromiumWebBrowser("https://gabrielecirulli.github.io/2048/")
            {
                Dock = DockStyle.Fill
            };
            pnlBrowser.Controls.Add(browser);
            var portList = SerialPort.GetPortNames();
            cmbPortName.Items.Clear();
            cmbPortName.Items.AddRange(portList);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
