using System;
using System.Text;
using System.Threading;
using SharpPcap.WinPcap;
using PacketDotNet;

namespace _tinyMonitor
{
    internal class Device
    {
        public static double upLoad = 0;                     //上传流量
        public static double downLoad = 0;                   //下载流量
        public static readonly object syncroot = new object();        //锁对象

        public static void getLoad()
        {
            while (true)
            {
                int vaildDev = 0;                                 //联网网卡
                int[] hasIP = new int[10];                        //联网标识数组
                var devices = Device.GetCurrentDevice(hasIP);     //网卡对象

                //创建线程对象，绑定线程函数
                Packet task;
                Thread[] thread = new Thread[10];                 //申请进程数组
                for (int i = 0; i < devices.Count; i++)           //遍历所有网卡
                {
                    /**如果网卡存在IP地址则创建相应线程**/
                    if (hasIP[i] == 1)
                    {
                        task = new Packet(devices, i);                                          //创建线程对象,并传递网卡参数
                        thread[vaildDev++] = new Thread(new ThreadStart(task.getPacket));       //委托线程函数
                    }
                }

                //开始线程
                for (int i = 0; i < vaildDev; i++)
                {
                    thread[i].Start();
                    while (!thread[i].IsAlive)
                        Thread.Sleep(10);
                }

                while (true)
                {
                    /**判断Packet线程运行个数，如果少于vaildDev重新创建**/
                    int thread_alive = 0;
                    for (int i = 0; i < vaildDev; i++)
                    {
                        if (thread[i].IsAlive)
                            thread_alive++;
                    }

                    /**每次循环都重新检测网卡数量,如果发生改变则重新创建网卡线程**/
                    if (vaildDev == thread_alive)
                    {
                        /****将前一秒的流量保存，以免刷新与生成气泡的时间差导致读取流量不足一秒****/
                        MainWindow.pre_downLoad = downLoad;
                        MainWindow.pre_upLoad = upLoad;
                        /**刷新流量值, 限定每秒打印一次流量信息**/
                        Device.refresh();
                        Thread.Sleep(1000);
                    }
                    /**如果线程获取数据包发生错误**/
                    else
                    {
                        /**停止所有网卡线程**/
                        for (int i = 0; i < vaildDev; i++)
                            if (thread[i].IsAlive)
                                thread[i].Abort();
                        break;  //重新检测网卡并创建网卡线程
                    }
                }

                /*销毁new对象*/
                thread = null;
                task = null;
                devices = null;
                hasIP = null;
            }
        }

        public static WinPcapDeviceList GetCurrentDevice(int[] hasIP)
        {
            int i = 0;
            var devlist = WinPcapDeviceList.New();                  //获得网卡指针
            //var devlist = WinPcapDeviceList.instance;             //这样写的话,就无法即时更新网卡数量和信息
            foreach (var dev in devlist)
            {
                int temp = dev.Addresses.Count;
                try
                {
                    if (string.Compare(dev.Addresses[temp - 2].Addr.ipAddress.ToString(), "0.0.0.0") == 1)    //如果网卡IP不为0
                        hasIP[i++] = 1;
                    else hasIP[i++] = 0;
                }
                catch
                {
                    hasIP[i++] = 0;
                    continue;
                }
            }
            return devlist;
        }

        public static void refresh()
        {
            lock (syncroot)
            {
                upLoad = 0;
                downLoad = 0;
            }
        }
    }
}
