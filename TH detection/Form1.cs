using System;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace TH_detection
{
    public partial class Form1 : Form
	{
		private StringBuilder sb = new StringBuilder();     //为了避免在接收处理函数中反复调用，依然声明为一个全局变量
		private StringBuilder builder = new StringBuilder();    //避免在事件处理方法中反复创建，定义为全局

		public class MonitoringData     //声明一个存放检测数据的类
		{
			public static int T = 0, H = 0;     //定义两个静态变量,T-温度 H-湿度
		}

		Random rnd = new Random();      //随机数（测试用）---> rnd.Next(500)

		public Form1()
		{
			InitializeComponent();  //初始化窗口
			serialPort1.BaudRate = 9600;    
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());    //获取电脑当前可用串口并添加到选项列表中
		}

		/*******************************************************************************
		* 函数名		：button1_Click
		* 函数功能		：“打开串口”按钮点击事件
		* 输入			：无
		* 输出			：无
		* 作者			：ChristinAdol
		*******************************************************************************/
		private void button1_Click(object sender, EventArgs e)
		{
			try
			{
				//将可能产生异常的代码放置在try块中
				//根据当前串口属性来判断是否打开
				if (serialPort1.IsOpen)
				{
					//串口已经处于打开状态
					serialPort1.Close();    //关闭串口
					button1.Text = "开始检测";
					comboBox1.Enabled = true;
					timer1.Stop();
					label3.Text = "无";
					label4.Text = "无";
				}
				else
				{
					//串口已经处于关闭状态，则设置好串口属性后打开
					comboBox1.Enabled = false;
					serialPort1.PortName = comboBox1.Text;
					serialPort1.BaudRate = 9600;
					serialPort1.DataBits = 8;
					serialPort1.Parity   = System.IO.Ports.Parity.None;
					serialPort1.StopBits = System.IO.Ports.StopBits.One;

					serialPort1.Open();     //打开串口
					button1.Text = "停止检测";
					timer1.Start();
				}
			}
			catch (Exception ex)        //捕获可能发生的异常并进行处理
			{
				//捕获到异常，创建一个新的对象，之前的不可以再用
				serialPort1 = new System.IO.Ports.SerialPort();
				//刷新COM口选项
				comboBox1.Items.Clear();
				comboBox1.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
				//响铃并显示异常给用户
				System.Media.SystemSounds.Beep.Play();
				button1.Text = "开始检测";
				MessageBox.Show(ex.Message);
				comboBox1.Enabled = true;

			}
		}

		/*******************************************************************************
		* 函数名		：SerialPort1_DataReceived
		* 函数功能		：串口接收处理程序
		* 输入			：无
		* 输出			：无
		* 作者			：ChristinAdol
		*******************************************************************************/
		private void SerialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
		{

			int num = serialPort1.BytesToRead;      //获取接收缓冲区中的字节数
			byte[] received_buf = new byte[num];    //声明一个大小为num的字节数据用于存放读出的byte型数据
			serialPort1.Read(received_buf, 0, num);   //读取接收缓冲区中num个字节到byte数组中

			sb.Clear();     //防止出错,首先清空字符串构造器
			sb.Append(Encoding.ASCII.GetString(received_buf));

			if (num != 0)
			{
				MonitoringData.T = sb[0];
				MonitoringData.H = sb[1];
			}


			try
			{
				//因为要访问UI资源，所以需要使用invoke方式同步ui
				Invoke((EventHandler)(delegate
				{
					label3.Text = (MonitoringData.T.ToString() + " ℃");
					label4.Text = (MonitoringData.H.ToString() + " %");
				}
				  )
				);
			}
			catch (Exception ex)
			{
				//响铃并显示异常给用户
				System.Media.SystemSounds.Beep.Play();
				MessageBox.Show(ex.Message);
			}
		}

		/*******************************************************************************
		* 函数名		：AddNewPoint()
		* 函数功能		：为chart添加新的点
		* 输入			：timeStamp、ptSeries、value
		* 输出			：无
		* 作者			：ChristinAdol
		*******************************************************************************/
		private void AddNewPoint(DateTime timeStamp, Series ptSeries, int value)
		{
			//将新数据点添加到其系列中
			ptSeries.Points.AddXY(timeStamp.ToOADate(), value);

			double removePoint = timeStamp.AddMinutes((double)(9) * (-1)).ToOADate();      //设定当前时间9min前的点

			while (ptSeries.Points[0].XValue < removePoint)
			{
				ptSeries.Points.RemoveAt(0);    //删除超过当前时间9min前的点。
			}
			//设置 chart1 x轴时间值
			chart1.ChartAreas[0].AxisX.Minimum = ptSeries.Points[0].XValue;     
			chart1.ChartAreas[0].AxisX.Maximum = DateTime.FromOADate(ptSeries.Points[0].XValue).AddMinutes(10).ToOADate();    
			//设置 chart2 x轴时间值
			chart2.ChartAreas[0].AxisX.Minimum = ptSeries.Points[0].XValue;     
			chart2.ChartAreas[0].AxisX.Maximum = DateTime.FromOADate(ptSeries.Points[0].XValue).AddMinutes(10).ToOADate();    

			//chart1.Invalidate();
			//chart2.Invalidate();
		}

		/*******************************************************************************
		* 函数名		：timer1_Tick
		* 函数功能		：定时调用描点函数
		* 输入			：无
		* 输出			：无
		* 作者			：ChristinAdol
		*******************************************************************************/
		private void timer1_Tick(object sender, EventArgs e)
		{
			DateTime timeStamp = DateTime.Now;      //获取当前时间到timeStamp
			AddNewPoint(timeStamp, chart1.Series[0], MonitoringData.T);
			AddNewPoint(timeStamp, chart2.Series[0], MonitoringData.H);
		}

		/*******************************************************************************
		* 函数名		：timer2_Tick
		* 函数功能		：定时更新显示时间
		* 输入			：无
		* 输出			：无
		* 作者			：ChristinAdol
		*******************************************************************************/
		private void timer2_Tick(object sender, EventArgs e)
		{
			label6.Text = System.DateTime.Now.ToString();
		}
	}
}
