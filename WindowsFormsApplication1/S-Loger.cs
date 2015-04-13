using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        static int number = 0;

        const char STX = (char)0x02;
        const char ETX = (char)0x03; //End Text [응답용Asc]
        const char EOT = (char)0x04; //End of Text[요구용 Asc]
        const char ENQ = (char)0x05; //Enquire[프레임시작코드]
        const char ACK = (char)0x06; //Acknowledge[응답 시작]
        const char NAK = (char)0x15; //Not Acknoledge[에러응답시작]

        private static SerialPort m_serialPort;

        private List<double> _valueList1;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("Form load");
            CheckForIllegalCrossThreadCalls = false;
            setChart();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            number++;
            Debug.WriteLine("Hello = " + number);
        }

        private void openDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // Help Button
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // Exit Button
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }

        /// 
        /// 포트오픈
        /// 
        private void SerialPortOpen()
        {

            // m_serialPort = new SerialPort("COM3", 19200, Parity.None, 8, StopBits.One);
            m_serialPort = new SerialPort();

            m_serialPort.PortName = "COM3";             // TODO COM3 하드코딩
            m_serialPort.BaudRate = 19200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.DataBits = 8;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.ReadTimeout = 500;
            m_serialPort.WriteTimeout = 500;
            
            m_serialPort.Open();
            
            btnOpen.Enabled = false;

            new Thread(new ThreadStart(getTemper)).Start();
        }

        // 1초마다 데이터 가져오기
        public void getTemper()
        {
            while(true) {
                getData();
                Thread.Sleep(1000);
            }
        }

        /// 
        /// 포트 종료
        /// 
        private void SerialPortClose()
        {
            m_serialPort.Close();
            btnOpen.Enabled = true;
        }

        // 시리얼번호 요청
        private void getSerial_Click(object sender, EventArgs e)
        {
            getData();
        }

        delegate void setOutputData(string output);

        private void setOutput(string data)
        {
            if (lblOutputData.InvokeRequired)
            {
                setOutputData call = new setOutputData(setOutput);

            }else{
                lblOutputData.Text = data;
            }
        }
        
        // 데이터 가져오기
        public void getData()
        {
            
            DateTime t1 = DateTime.Now;
            string strOutputData = "ATCD\n";
            m_serialPort.Write(strOutputData);

            setOutput(strOutputData);
            
            do
            {
                //데이타를 전부 PLC로 전송 하기 위함..
            } while (m_serialPort.WriteBufferSize == 0);

            string indata = DataRead();
            
            double temper = Convert.ToDouble(indata.Substring(5, 4));

            Debug.WriteLine("temper = " + temper);

            addData(temper);

            TimeSpan span = DateTime.Now.Subtract(t1);
            lblMillisecond.Text = span.Milliseconds.ToString();
        }

        //실행
        private void btnExecute_Click(object sender, EventArgs e)
        {
            DateTime t1 = DateTime.Now;
            string strOutputData = ENQ + "00RSS01" + "07%MX0000" + EOT;
            m_serialPort.Write(strOutputData);
            lblOutputData.Text = strOutputData;
            do
            {
                //데이타를 전부 PLC로 전송 하기 위함..
            } while (m_serialPort.WriteBufferSize == 0);
            
            string indata = DataRead();

            TimeSpan span = DateTime.Now.Subtract(t1);
            lblMillisecond.Text = span.Milliseconds.ToString();
        }
 
        /// 
        /// PLC로 부터 수신된 데이타를 가지고 온다
        /// 
        /// 
        private string DataRead()
        {
            bool m_Next = false;
            string strInData = string.Empty;
            string strRetValue= string.Empty;
        
            DateTime start = DateTime.Now;
            do
            {
                string msg = m_serialPort.ReadExisting();

                strInData += msg;
                //TODO : 데이타에 종료문자가 있으면...
                if (msg.IndexOf(ETX) > 0)
                {
                    //TODO 데이타 처음에 정상 응답이 있으면
                    if (strInData[0] == ACK)
                    {
                        //TODO 들어오는 데이타를 분석..[ETX(1)+국번(2)+비트읽기(3)+블륵수(2)]
                        lblInputData.Text = strInData;//Test용
                        strRetValue = strInData.Substring(8, strInData.Length - 9); //실제Data
                        m_Next = true;
                    }
                    //TODO: 데이타에 비정상 응답이 들어오면..
                    else if (strInData[0] == NAK)
                    {
                        lblInputData.Text = "NAK";
                        strRetValue = "-1";
                        m_Next = true;
                    }
                }
                //DOTO : 응답이 없으면 0.5초간은 로프를둘면서 기다란다.
                TimeSpan ts = DateTime.Now.Subtract(start);
                if (ts.Milliseconds > 500)
                {
                    //lblInputData.Text = "TimeOut";
                    //lblInputData.Text = strInData;

                    strRetValue = "-3";
                    m_Next = true;
                }
            } while (!m_Next);

            //Debug.WriteLine("strInData = " + strInData);

            return strInData;
        }

        // 연결
        private void btnOpen_Click(object sender, EventArgs e)
        {
            SerialPortOpen();
        }

        // 연결 종료
        private void btnClose_Click(object sender, EventArgs e)
        {
            SerialPortClose();
        }

        // 그래프 초기 세팅
        private void setChart()
        {
            chart1.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
            chart1.ChartAreas[0].AxisX.ScaleView.SizeType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.Interval = 0;
            chart1.ChartAreas[0].AxisY.Minimum = 23; // 최하온도
            chart1.ChartAreas[0].AxisY.Maximum = 30; // 최대온도
            _valueList1 = new List<double>();
            DateTime now = DateTime.Now;
            chart1.ChartAreas[0].AxisX.Minimum = now.ToOADate();
            chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(60).ToOADate();
        }

        // Data 추가
        private void addData(double data)
        {
            //_valueList1.Add(System.DateTime.Now.Second);

            Debug.WriteLine("data = " + data);
            Debug.WriteLine("chart1 = " + chart1);

            if (chart1 == null)
            {
                return;
            }

            Debug.WriteLine("chart1.Series[0] = " + chart1.Series[0]);

            _valueList1.Add(data);
            DateTime now = DateTime.Now;

            if (chart1.Series[0].Points.Count > 0)
            {
                while (chart1.Series[0].Points[0].XValue < now.AddSeconds(-60).ToOADate())
                {
                    chart1.Series[0].Points.RemoveAt(0);
                    chart1.ChartAreas[0].AxisX.Minimum = chart1.Series[0].Points[0].XValue;
                    chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(0).ToOADate();
                }
            }
            chart1.Series[0].Points.AddXY(now.ToOADate(), _valueList1[_valueList1.Count - 1]);
            chart1.Invalidate();
        }
    }
}
