using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class FrmChangePort : Form
    {

        public delegate void SendPortNum(string port);
        public event SendPortNum SendPort;

        public FrmChangePort()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SendPort(portText.Text);
            this.Close();
        }
    }
}
