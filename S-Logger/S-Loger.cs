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
    public partial class FrmMain : Form
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

        // TODO Port 기본값 COM3
        public string _port = "COM3";

        public FrmMain()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            // 차트 초기 세팅
            setChart();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            number++;
        }

        private void openDataToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // Help Button
        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        // 종료 Button
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.Application.Exit();
        }
        
        // 포트변경
        private void changePortToolStripMenuItem_Click(object sender, EventArgs e)
        {

            FrmChangePort _frmChangePort = new FrmChangePort();
            _frmChangePort.portText.Text = _port;

            _frmChangePort.SendPort += new FrmChangePort.SendPortNum(setPort);

            _frmChangePort.Show();
        }

        void setPort(string port)
        {
            _port = port;
        }

        // 그래프 초기 세팅
        private void setChart()
        {
            //chart1.Series.Add("Temper");

            //chart1.Series[0].ChartType = SeriesChartType.Line;
            //chart1.Series[0].XValueType = ChartValueType.Time;
            //chart1.Series[0].XAxisType = AxisType.Primary;
            //chart1.Series[0].Color = Color.Red;
            //chart1.ChartAreas[0].BorderDashStyle = ChartDashStyle.Solid;    /* Border 영역 줄 긋기 */

            chart1.ChartAreas[0].AxisX.IsStartedFromZero = true;
            chart1.ChartAreas[0].AxisX.ScaleView.Zoomable = false;
            chart1.ChartAreas[0].AxisX.ScaleView.SizeType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.IntervalAutoMode = IntervalAutoMode.FixedCount;
            chart1.ChartAreas[0].AxisX.IntervalType = DateTimeIntervalType.Seconds;
            chart1.ChartAreas[0].AxisX.Interval = 0;
            chart1.ChartAreas[0].AxisY.Minimum = 20; // 최하온도
            chart1.ChartAreas[0].AxisY.Maximum = 30; // 최대온도
            _valueList1 = new List<double>();
            DateTime now = DateTime.Now;
            chart1.ChartAreas[0].AxisX.Minimum = now.ToOADate();
            chart1.ChartAreas[0].AxisX.Maximum = now.AddSeconds(60).ToOADate();
        }

        // S-Logger 시작
        private void btnStart_Click(object sender, EventArgs e)
        {
            SerialPortOpen();
        }

        /// 
        /// 포트오픈
        /// 
        private void SerialPortOpen()
        {

            // m_serialPort = new SerialPort("COM3", 19200, Parity.None, 8, StopBits.One);
            m_serialPort = new SerialPort();

            m_serialPort.PortName = _port;             // TODO COM3 하드코딩
            m_serialPort.BaudRate = 19200;
            m_serialPort.Parity = Parity.None;
            m_serialPort.DataBits = 8;
            m_serialPort.StopBits = StopBits.One;
            m_serialPort.Handshake = Handshake.None;
            m_serialPort.ReadTimeout = 500;
            m_serialPort.WriteTimeout = 500;
            
            m_serialPort.Open();
            
            btnStart.Enabled = false;

            // 타이머 스레드 시작
            Thread t1 = new Thread(new ThreadStart(setTimer));
            t1.Start();

            // 데이터 가져오기 스레드 시작
            //Thread t2 = new Thread(new ThreadStart(getTemper));
            //t2.Start();

            // 데이터 가져오기
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1초
            timer.Tick += new EventHandler(getData);
            timer.Start();
        }

        // 타이머 표시
        public void setTimer()
        {
            Stopwatch SW = new Stopwatch();

            SW.Reset();
            SW.Start();

            // 현재 인스턴스가 측정한 총 경과 시간을 가져옵니다.
            string sTemp1 = SW.Elapsed.ToString().Substring(0, 8);
            lb_Timer.Text = sTemp1;

            while (true)
            {
                Thread.Sleep(1000);

                // 현재 인스턴스가 측정한 총 경과 시간을 가져옵니다.
                sTemp1 = SW.Elapsed.ToString().Substring(0, 8);
                lb_Timer.Text = sTemp1;
            }
        }

        // 1초마다 데이터 가져오기
        //public void getTemper()
        //{
        //    while(true) {
        //        getData();
        //        Thread.Sleep(1000);
        //    }
        //}

        // 데이터 가져오기
        void getData(object sender, EventArgs e)
        {

            DateTime t1 = DateTime.Now;
            string strOutputData = "ATCD\n";
            m_serialPort.Write(strOutputData);

            //setOutput(strOutputData);

            do
            {
                //데이타를 전부 PLC로 전송 하기 위함..
            } while (m_serialPort.WriteBufferSize == 0);

            string indata = DataRead();

            double temper = Convert.ToDouble(indata.Substring(5, 4));
            addData(temper);
        }

        /// 
        /// 포트 종료
        /// 
        private void SerialPortClose()
        {
            m_serialPort.Close();
            btnStart.Enabled = true;
        }
 
        /// 
        /// PLC로 부터 수신된 데이타를 가지고 온다
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
                        //lblInputData.Text = strInData;//Test용
                        strRetValue = strInData.Substring(8, strInData.Length - 9); //실제Data
                        m_Next = true;
                    }
                    //TODO: 데이타에 비정상 응답이 들어오면..
                    else if (strInData[0] == NAK)
                    {
                        //lblInputData.Text = "NAK";
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

        // Data 추가
        private void addData(double data)
        {
            //_valueList1.Add(System.DateTime.Now.Second);

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

            chart1.ChartAreas[0].AxisY.Minimum = data - 3; // 최하온도
            chart1.ChartAreas[0].AxisY.Maximum = data + 3; // 최대온도

            chart1.Series[0].Points.AddXY(now.ToOADate(), data);
            chart1.Invalidate();
        }

        //실행
        private void btnExecute_Click(object sender, EventArgs e)
        {
            DateTime t1 = DateTime.Now;
            string strOutputData = ENQ + "00RSS01" + "07%MX0000" + EOT;
            m_serialPort.Write(strOutputData);
            do
            {
                //데이타를 전부 PLC로 전송 하기 위함..
            } while (m_serialPort.WriteBufferSize == 0);

            string indata = DataRead();

            TimeSpan span = DateTime.Now.Subtract(t1);
            //lblMillisecond.Text = span.Milliseconds.ToString();
        }
    }
}
