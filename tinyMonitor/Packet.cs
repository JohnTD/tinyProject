using System;
using System.Text;
using System.Threading;
using SharpPcap.WinPcap;
using PacketDotNet;

namespace _tinyMonitor
{
    internal class Packet
    {
        private WinPcapDeviceList devices;
        private int num;

        public Packet(WinPcapDeviceList Devices, int Num)
        {
            devices = Devices;
            num = Num;
        }

        public void getPacket()
        {
            var dev = devices[num];                                                                       //获取网卡指针
            try
            {
                int temp = dev.Addresses.Count;
                string localMac = dev.Addresses[temp - 1].Addr.hardwareAddress.ToString();                     //获取网卡物理地址
                int readTimeoutMilliseconds = 1000;                                                            //设置等待超时时间

                SharpPcap.RawCapture pkt;
                dev.Open(OpenFlags.Promiscuous, readTimeoutMilliseconds);                                     //打开网卡

                while (true)
                {
                    if ((pkt = dev.GetNextPacket()) != null)                                                   //截取数据包
                    {
                        var packet = PacketDotNet.Packet.ParsePacket(pkt.LinkLayerType, pkt.Data);             //转换数据包格式

                        if (packet is PacketDotNet.EthernetPacket)                                             //是否为以太网数据包
                        {
                            var eth = ((PacketDotNet.EthernetPacket)packet);                                   //转换为EthernetPacket格式
                            if (eth != null)
                            {
                                if (string.Compare(localMac, eth.SourceHwAddress.ToString()) == 0)                //发送数据包
                                {
                                    lock (Device.syncroot)                                                       //上锁
                                        Device.upLoad += double.Parse(pkt.Data.Length.ToString()) / 1024;        //更新总上传流量
                                }
                                else                                                                              //接收数据包
                                {
                                    lock (Device.syncroot)                                                       //上锁
                                        Device.downLoad += double.Parse(pkt.Data.Length.ToString()) / 1024;      //更新总下载流量
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                dev.Close();
                Thread.CurrentThread.Abort();
            }
        }
    }
}
