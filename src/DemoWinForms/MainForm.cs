using System;
using System.Drawing;
using System.Windows.Forms;

namespace DemoWinForms
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            FormBorderStyle = FormBorderStyle.None;

            labTitle.Text = "我是窗体标题栏";
            labTitle.MouseDown += TitleBar_MouseDown;
            labTitle.MouseMove += TitleBar_MouseMove;
            splitContainer1.Panel1.MouseDown += TitleBar_MouseDown;
            splitContainer1.Panel1.MouseMove += TitleBar_MouseMove;
        }


        private Point mouseOffSet;

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            // 记录鼠标按下时的位置（这是相对于当前窗体左上角的位置）
            mouseOffSet = new Point(e.X, e.Y);
        }

        private void TitleBar_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // 获取鼠标位置
                Point mousePosition = Control.MousePosition;
                // 减去鼠标按下的位置偏移量
                mousePosition.Offset(-mouseOffSet.X, -mouseOffSet.Y);
                // 移动窗体
                Location = mousePosition;
            }
        }

        private void labSmall_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void labClose_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }
    }
}