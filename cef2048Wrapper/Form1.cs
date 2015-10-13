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
        enum Direction { left, right, up, down};

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
            this.Invoke((Action<object>)((obj) =>
            {
                SerialPort sp = (SerialPort)sender;
                string indata = sp.ReadExisting();
                receivedData += indata;
            }), new Object());
        }

        private void consoleReceiveHandler(object sender, ConsoleMessageEventArgs e)
        {
            this.Invoke((Action<object>)((obj) =>
            {
                if (char.IsNumber(e.Message[0]))
                {
                    browserConsole.AppendText(e.Message + "\n");
                    startSearch(e.Message.Trim());
                }
            }), new Object());
        }

        private void startSearch(String cells)
        {
            int[,] inboard = cellsStrToBoard(cells);
            int[,] merged;
            merged = mergeBoard(inboard, Direction.left).Item1;
            for (var i = 0; i < 4; i++)
            {
                for(var j = 0; j < 4; j++)
                {
                    browserConsole.AppendText("up:");
                    browserConsole.AppendText(merged[3 - j, i] + " ");
                }
                browserConsole.AppendText("\n");
            }
          
        }

        private Tuple<int[,], bool> mergeBoard(int[,] board, Direction dir)
        {
            Tuple<int[,], bool> mergeAns;
            int[,] rotated = rotateBoard(board, dir);
            mergeAns = leftMergeBoard(rotated);
            return new Tuple<int[,], bool>(unrotateBoard(mergeAns.Item1, dir), mergeAns.Item2);
        }

        private Tuple<int[,],bool> leftMergeBoard(int[,] board)
        {
            int[,] merged = new int[4, 4];
            bool movable = true;
            Tuple<int[], bool> mLine;
            mLine = lineMerge(board[3,0], board[2,0], board[1,0], board[0,0]);
            movable = movable & mLine.Item2;
            for(var i = 0; i < 4; i++)
            {
                merged[i, 0] = mLine.Item1[3-i];
            }
            mLine = lineMerge(board[3,1], board[2,1], board[1,1], board[0,1]);
            movable = movable & mLine.Item2;
            for(var i = 0; i < 4; i++)
            {
                merged[i, 1] = mLine.Item1[3-i];
            }
            mLine = lineMerge(board[3,2], board[2,2], board[1,2], board[0,2]);
            movable = movable & mLine.Item2;
            for(var i = 0; i < 4; i++)
            {
                merged[i, 2] = mLine.Item1[3-i];
            }
            mLine = lineMerge(board[3,3], board[2,3], board[1,3], board[0,3]);
            movable = movable & mLine.Item2;
            for(var i = 0; i < 4; i++)
            {
                merged[i, 3] = mLine.Item1[3-i];
            }
            return new Tuple<int[,], bool>(merged, movable);
        }

        private int[,] cellsStrToBoard(String cells)
        {
            int[] cellsSeq = cells.Split(' ').Select(elm => int.Parse(elm)).ToArray<int>();
            //map every cell (log 2)
            for(var i = 0; i < 16; i++)
            {
                if(cellsSeq[i] == 0)
                {
                    continue;
                }
                var cnt = 0;
                while(cellsSeq[i] != 1)
                {
                    cellsSeq[i] /= 2;
                    cnt += 1;
                }
                cellsSeq[i] = cnt;
            }
            int[,] board = new int[4,4];
            for(var i = 0; i < 16; i++)
            {
                board[3 - i / 4, i % 4] = cellsSeq[i];
            }
            return board;
        }

        private int[,] rotateBoard(int[,] board, Direction dir)
        {
            int[,] rotated = new int[4, 4];
            if (dir == Direction.left)
            {
                rotated = board;
            }
            else if(dir == Direction.right)
            {
                rotated = hMirrorBoard(board);
            }
            else if(dir == Direction.up)
            {
                rotated = transposeBoard(board);
            }
            else if(dir == Direction.down)
            {
                rotated = transposeBoard(hMirrorBoard(board));
            }
            return rotated;
        }

        private int[,] unrotateBoard(int[,] board, Direction dir)
        {
            int[,] rotated = new int[4, 4];
            if (dir == Direction.left)
            {
                rotated = board;
            }
            else if(dir == Direction.right)
            {
                rotated = hMirrorBoard(board);
            }
            else if(dir == Direction.up)
            {
                rotated = transposeBoard(board);
            }
            else if(dir == Direction.down)
            {
                rotated = hMirrorBoard(transposeBoard(board));
            }
            return rotated;

        }

        private int[,] hMirrorBoard(int[,] board)
        {
            int[,] mirrored = new int[4, 4];
            for(var i = 0; i < 4; i++)
            {
                for(var j = 0; j < 4; j++)
                {
                    mirrored[i, j] = board[3 - i, j];
                }
            }
            return mirrored;
        }

        private int[,] transposeBoard(int[,] board)
        {
            int[,] transposed = new int[4, 4];
            for(var i = 0; i < 4; i++)
            {
                for(var j = 0; j < 4; j++)
                {
                    transposed[i, j] = board[j, i];
                }
            }
            return transposed;

        }

        private Tuple<int[], bool> lineMerge(int x0, int x1, int x2, int x3)
        {
            int y0 = 5, y1 = 5, y2 = 5, y3 = 5;
            bool movable = false;
            if (x0 == 0 && x1 == 0 && x2 == 0 && x3 == 0)
            {
                y0 = 0;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = false;
            }
            else if (x0 == 0 && x1 == 0 && x2 == 0 && x3 != 0)
            {
                y0 = x3;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 == 0 && x2 != 0 && x3 == 0)
            {
                y0 = x2;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 == 0 && x3 == 0)
            {
                y0 = x1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 == 0 && x3 == 0)
            {
                y0 = x0;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = false;
            }
            else if (x0 == 0 && x1 == 0 && x2 != 0 && x3 != 0 && x2 != x3)
            {
                y0 = x2;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 == 0 && x3 != 0 && x1 != x3)
            {
                y0 = x1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 == 0 && x3 != 0 && x0 != x3)
            {
                y0 = x0;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 == 0 && x1 != x2)
            {
                y0 = x1;
                y1 = x2;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 == 0 && x0 != x2)
            {
                y0 = x0;
                y1 = x2;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 == 0 && x0 != x1)
            {
                y0 = x0;
                y1 = x1;
                y2 = 0;
                y3 = 0;
                movable = false;
            }
            else if (x0 == 0 && x1 == 0 && x2 != 0 && x3 != 0 && x2 == x3)
            {
                y0 = x2 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 == 0 && x3 != 0 && x1 == x3)
            {
                y0 = x1 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 == 0 && x3 != 0 && x0 == x3)
            {
                y0 = x0 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 == 0 && x1 == x2)
            {
                y0 = x1 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 == 0 && x0 == x2)
            {
                y0 = x0 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 == 0 && x0 == x1)
            {
                y0 = x0 + 1;
                y1 = 0;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 != 0 && x1 != x2 && x2 != x3)
            {
                y0 = x1;
                y1 = x2;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 != 0 && x0 != x2 && x2 != x3)
            {
                y0 = x0;
                y1 = x2;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 != 0 && x0 != x1 && x1 != x3)
            {
                y0 = x0;
                y1 = x1;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 == 0 && x0 != x1 && x1 != x2)
            {
                y0 = x0;
                y1 = x1;
                y2 = x2;
                y3 = 0;
                movable = false;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 != 0 && x1 == x2 && x2 != x3)
            {
                y0 = x1 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 != 0 && x0 == x2 && x2 != x3)
            {
                y0 = x0 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 != 0 && x0 == x1 && x1 != x3)
            {
                y0 = x0 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 == 0 && x0 == x1 && x1 != x2)
            {
                y0 = x0 + 1;
                y1 = x2;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 != 0 && x1 != x2 && x2 == x3)
            {
                y0 = x1;
                y1 = x2 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 != 0 && x0 != x2 && x2 == x3)
            {
                y0 = x0;
                y1 = x2 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 != 0 && x0 != x1 && x1 == x3)
            {
                y0 = x0;
                y1 = x1 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 == 0 && x0 != x1 && x1 == x2)
            {
                y0 = x0;
                y1 = x1 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 == 0 && x1 != 0 && x2 != 0 && x3 != 0 && x1 == x2 && x2 == x3)
            {
                y0 = x1 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 == 0 && x2 != 0 && x3 != 0 && x0 == x2 && x2 == x3)
            {
                y0 = x0 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 == 0 && x3 != 0 && x0 == x1 && x1 == x3)
            {
                y0 = x0 + 1;
                y1 = x3;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 == 0 && x0 == x1 && x1 == x2)
            {
                y0 = x0 + 1;
                y1 = x2;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 != x1 && x1 != x2 && x2 != x3)
            {
                y0 = x0;
                y1 = x1;
                y2 = x2;
                y3 = x3;
                movable = false;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 == x1 && x1 != x2 && x2 != x3)
            {
                y0 = x0 + 1;
                y1 = x2;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 != x1 && x1 == x2 && x2 != x3)
            {
                y0 = x0;
                y1 = x1 + 1;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 == x1 && x1 == x2 && x2 != x3)
            {
                y0 = x0 + 1;
                y1 = x2;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 != x1 && x1 != x2 && x2 == x3)
            {
                y0 = x0;
                y1 = x1;
                y2 = x2 + 1;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 == x1 && x1 != x2 && x2 == x3)
            {
                y0 = x0 + 1;
                y1 = x2 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 != x1 && x1 == x2 && x2 == x3)
            {
                y0 = x0;
                y1 = x1 + 1;
                y2 = x3;
                y3 = 0;
                movable = true;
            }
            else if (x0 != 0 && x1 != 0 && x2 != 0 && x3 != 0 && x0 == x1 && x1 == x2 && x2 == x3)
            {
                y0 = x0 + 1;
                y1 = x2 + 1;
                y2 = 0;
                y3 = 0;
                movable = true;
            }
            return new Tuple<int[], bool>(new int[] { y0, y1, y2, y3 }, movable);
        }
    }
}
    
