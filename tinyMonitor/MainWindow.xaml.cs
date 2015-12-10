using System;
using System.Text;
using System.Text.RegularExpressions;             //正则表达式类
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using SharpPcap.WinPcap;
using PacketDotNet;                               //网络数据包解析与格式转换

namespace _tinyMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon notifyIcon;                                          //托盘对象
        private System.Timers.Timer updateLoad = new System.Timers.Timer();     //时间对象

        /****将前一秒的流量保存，以免刷新与生成气泡的时间差导致读取流量不足一秒****/
        public static double pre_upLoad = 0;                     //上传流量
        public static double pre_downLoad = 0;                   //下载流量

        public static Wifi wifi = new Wifi();                          //wifi连接子线程
        
        public MainWindow()
        {
            //创建流量处理线程
            Thread p_thread = new Thread(new ThreadStart(Device.getLoad));
            p_thread.Start();
            while (!p_thread.IsAlive)
            {
                Thread.Sleep(10);
            }

            InitNotifyIcon();                                                          //初始化托盘参数
            
            wifi.ScanSSID();
            wifi.client.Interfaces[0].Disconnect();
            wifi.deleteAllProfiles();

            wifi.current_checkbox.Checked = true;
            wifi.AutoConnection();
            
            updateLoad.Interval = 1000;                                                //时间间隔
            updateLoad.Elapsed += new System.Timers.ElapsedEventHandler(Timer_Tick);   //绑定时间函数
        }

        private void InitNotifyIcon()
        {
            this.Visibility = System.Windows.Visibility.Hidden;                                   //将主页面隐藏

            //初始化托盘对象
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Windows.Forms.Application.ExecutablePath);    //使用默认托盘图标
            notifyIcon.Visible = true;

            //寻找wifi,显示流量气泡
            this.notifyIcon.Click += new System.EventHandler(wifi_device);     //这里使用Click或者MouseClick会导致任何点击调用wifi_device函数，MouseDown则需要将气泡点掉
            this.notifyIcon.MouseMove += new System.Windows.Forms.MouseEventHandler(notifyIcon_MouseMove);    //鼠标移动到图标显示流量气泡
            this.notifyIcon.BalloonTipClosed += new System.EventHandler(notifyIcon_BalloonTipClosed);
            
            //设置右键菜单
            //System.Windows.Forms.MenuItem home = new System.Windows.Forms.MenuItem("主页面");
            //home.Click += new System.EventHandler(Show_Home);
            System.Windows.Forms.MenuItem web_share = new System.Windows.Forms.MenuItem("打开网络和共享中心");
            web_share.Click += new EventHandler(Web_And_Share);
            System.Windows.Forms.MenuItem close = new System.Windows.Forms.MenuItem("关闭");
            close.Click += new System.EventHandler(Close);

            //关联托盘控件
            //System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { home, web_share, close };
            System.Windows.Forms.MenuItem[] childen = new System.Windows.Forms.MenuItem[] { web_share, close };
            notifyIcon.ContextMenu = new System.Windows.Forms.ContextMenu(childen);
        }

        //气泡时间函数
        void Timer_Tick(object sender, EventArgs e)
        {
            notifyIcon.ShowBalloonTip(20, "tinyMonitor", "DownLoad:" + pre_downLoad.ToString("F2") + "KB/s\n" + "UpLoad:" + pre_upLoad.ToString("F2") + "KB/s\n" + "TotalLoad:" + (pre_downLoad + pre_upLoad).ToString("F2") + "KB/s", ToolTipIcon.Info);
        }

        //气泡函数-移入
        private void notifyIcon_MouseMove(object sender, EventArgs e)
        {
            
            //if (((System.Windows.Forms.MouseEventArgs)e).X <= )
                updateLoad.Enabled = true;                                         //时间对象使能
        }

        //气泡函数-关闭
        private void notifyIcon_BalloonTipClosed(object sender, EventArgs e)   //当移开托盘图标后一直被调用
        {
            updateLoad.Enabled = false;                                        //时间对象使不能
        }
        
        //右键菜单-主页面
        private void Show_Home(object sender, EventArgs e)
        {
            this.Visibility = System.Windows.Visibility.Visible;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        //右键菜单-打开网络和共享中心
        private void Web_And_Share(object sender, EventArgs e)
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();     //创建进程调用外部函数运行
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "control.exe";                                  //执行程序
            startInfo.Arguments = "/name Microsoft.NetworkAndSharingCenter";     //执行参数

            p.StartInfo = startInfo;                            //绑定startInfo
            p.Start();                                          //开始进程
            p.WaitForExit();                                    //等待进程结束
            p.Close();                                          //关闭进程
        }

        void wifi_device(object sender, EventArgs e)
        {
            /**notifyicon的icon为鼠标左键点击时触发**/
            if(((System.Windows.Forms.MouseEventArgs)e).Button == MouseButtons.Left)
            {
                wifi.Controls.Clear();
                wifi.InitializeComponent();
                wifi.ScanSSID();
                wifi.show_wifi();
            }
        }

        //关闭
        private void Close(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
