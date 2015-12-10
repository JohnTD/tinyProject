using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using NativeWifi;

namespace _tinyMonitor
{
    public partial class Wifi : Form
    {
        public WlanClient client = new WlanClient();
        private Panel current_panel = new Panel();                  //指向当前点击的Panel
        private Panel current_connect_panel = new Panel();          //指向当前连接的Panel
        private TextBox current_textbox = new TextBox();            //保存输入的wifi密码
        public CheckBox current_checkbox = new CheckBox();         //保存自动连接的设置
        private Label current_key_err = new Label();                //显示输入密码错误
        private WIFISSID current_connect_ssid = new WIFISSID();     //当前连接的ssid
        private Panel pre_panel = new Panel();                      //指向之前点击的panel
        public static readonly object syncroot = new object();      //锁对象

        public Wifi()
        {
            /**创建或打开软件的无线配置文件**/
            if (!File.Exists("profile.xml"))
            {
                FileStream fs = new FileStream("profile.xml", FileMode.Create);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine("<?xml version=\"1.0\"?>");
                sw.WriteLine("<ProfileXml>");
                sw.WriteLine("</ProfileXml>");
                sw.Close();
                fs.Close();
            }
        }

        public class WIFISSID
        {
            public string SSID = "NONE";
            public Wlan.Dot11AuthAlgorithm dot11DefaultAuthAlgorithm = Wlan.Dot11AuthAlgorithm.IEEE80211_Open;       //认证方式
            public Wlan.Dot11CipherAlgorithm dot11DefaultCipherAlgorithm = Wlan.Dot11CipherAlgorithm.CCMP;           //加密方式
            public bool networkConnectable = true;
            public int wlanSignalQuality = 0;
            public WlanClient.WlanInterface wlanInterface = null;
        }

        //Wifi信息列表
        public List<WIFISSID> ssids = new List<WIFISSID>();

        /**扫描wifi并添加ssid链表, 并设为static, 用于软件开启后自动扫描wifi**/
        public void ScanSSID()
        {
            ssids = new List<WIFISSID>();
            /***遍历所有的无线设备接口****/
            foreach (WlanClient.WlanInterface wlanIface in client.Interfaces)
            {
                /****这里获得的wifi会与无线配置文件叠加，因此需做处理****/
                Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
                foreach (Wlan.WlanAvailableNetwork network in networks)
                {
                    /*判断ssid是否有对应的配置文件*/
                    bool flag = true;
                    if (GetStringForSSID(network.dot11Ssid) == "0")
                        continue;
                    WIFISSID targetSSID = new WIFISSID();
                    targetSSID.wlanInterface = wlanIface;
                    targetSSID.wlanSignalQuality = (int)network.wlanSignalQuality;
                    targetSSID.SSID = GetStringForSSID(network.dot11Ssid);
                    targetSSID.dot11DefaultAuthAlgorithm = network.dot11DefaultAuthAlgorithm;
                    targetSSID.dot11DefaultCipherAlgorithm = network.dot11DefaultCipherAlgorithm;
                    /*遍历ssids,通过ssid和认证方式判断是否重叠*/
                    foreach (var tmp in ssids)
                        /***对于多个同名的ssid，只保存两个不同认证方式的ssid***/
                        if (tmp.SSID == targetSSID.SSID && tmp.dot11DefaultAuthAlgorithm == targetSSID.dot11DefaultAuthAlgorithm) flag = false;
                    if(flag)
                        ssids.Add(targetSSID);
                }
            }
        }

        public void show_wifi()
        {
            //将Wifi窗口置于右下角
            Location = new System.Drawing.Point(
                System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Width - this.Width - 180,
                System.Windows.Forms.Screen.PrimaryScreen.WorkingArea.Height - this.Height);

            string CurDir = System.Environment.CurrentDirectory;

            Deactivate += new System.EventHandler(hide);

            //绑定刷新函数
            refresh.Click += new EventHandler(refresh_wifi);

            //创建wifi按钮
            FlowLayoutPanel wifi_panel = new FlowLayoutPanel();
            wifi_panel.Size = new System.Drawing.Size(290, 330);
            wifi_panel.Location = new Point(0, 40);
            wifi_panel.AutoScroll = true;
            int item_Num = 0;
            foreach (var wifi in ssids)
            {
                Panel item = new Panel();
                item.Location = new Point(0, item_Num * 30);
                item.Size = new System.Drawing.Size(260, 30);
                /**将panel和button的Name都设为ssid+认证方式,解决同名ssid的一一对应**/
                item.Name = wifi.SSID.ToString() + "认证方式" + wifi.dot11DefaultAuthAlgorithm.ToString();
                /************添加wifi按钮***********/
                Button bt = new Button();
                bt.Location = new Point(0, 0);
                bt.Size = new Size(260, 30);
                bt.Name = wifi.SSID.ToString() + "认证方式" + wifi.dot11DefaultAuthAlgorithm.ToString();

                /**限制Button的Text长度**/
                if (wifi.SSID.Length > 15)
                    bt.Text = wifi.SSID.ToString().Substring(0, 15) + "...";
                else
                {
                    /**根据软件无线配置文件名为button赋值**/
                    bool hasSameProfile = false;
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load("profile.xml");
                    if (xmlDoc != null)
                    {
                        XmlNodeList listNodes = xmlDoc.SelectNodes("/ProfileXml/SSID");
                        if (listNodes != null && listNodes.Count != 0)
                        {
                            foreach (XmlNode node in listNodes)
                            {
                                if (node.ChildNodes[1].InnerText == wifi.SSID.ToString() && node.ChildNodes[2].InnerText == wifi.dot11DefaultAuthAlgorithm.ToString())
                                {
                                    bt.Text = node.ChildNodes[0].InnerText;
                                    hasSameProfile = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (!hasSameProfile)
                        bt.Text = wifi.SSID.ToString();
                }
                bt.TextAlign = ContentAlignment.TopLeft;

                /***信号强度***/
                int signal = wifi.wlanSignalQuality;

                if (wifi.wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                {
                    string profilexml = ssids[0].wlanInterface.GetProfileXml(ssids[0].wlanInterface.CurrentConnection.profileName);
                    string[] str = GetProfileInfo(profilexml);
                    if (wifi.SSID == str[0] && wifi.dot11DefaultAuthAlgorithm.ToString() == str[1])
                    {
                        if ((signal > 80) && (signal <= 100))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\8.png");
                        else if ((signal > 60) && (signal <= 80))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\7.png");
                        else if ((signal > 40) && (signal <= 60))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\6.png");
                        else
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\5.png");
                    }
                    else
                    {
                        if ((signal > 80) && (signal <= 100))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\4.png");
                        else if ((signal > 60) && (signal <= 80))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\3.png");
                        else if ((signal > 40) && (signal <= 60))
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\2.png");
                        else
                            bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\1.png");
                    }
                }
                else
                {
                    if ((signal > 80) && (signal <= 100))
                        bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\4.png");
                    else if ((signal > 60) && (signal <= 80))
                        bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\3.png");
                    else if ((signal > 40) && (signal <= 60))
                        bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\2.png");
                    else
                        bt.Image = Image.FromFile(CurDir + "\\..\\..\\wifi\\1.png");
                }
                
                bt.FlatStyle = FlatStyle.Flat;
                bt.BackColor = Color.Transparent;
                bt.FlatAppearance.BorderSize = 0;
                
                bt.ImageAlign = ContentAlignment.TopRight;
                bt.Click += new EventHandler(ready_to_connect);

                /***关联控件***/
                wifi_panel.Controls.Add(item);
                item.Controls.Add(bt);

                item_Num++;
            }
            Controls.Add(wifi_panel);
            Show();                            //显示wifi窗口
        }

        //显示wifi信息
        private void ready_to_connect(object sender, EventArgs e)
        {
            Button curr_bt = sender as Button;
            string[] bt_info = Regex.Split(curr_bt.Name, "兠凟剅吺唗投斣枓", RegexOptions.IgnoreCase);
            /**获得FlowLayoutPanel控件**/
            FlowLayoutPanel panel = (FlowLayoutPanel)this.GetChildAtPoint(new Point(0, 40));
            var tmp = panel.GetNextControl((System.Windows.Forms.Control)panel, true);
            while(true)
            {
                if(tmp == null) return;
                else if(tmp is Panel)
                {
                    /**找到与当前点击button对应的panel,并保存为current_panel**/
                    if(tmp.Name == curr_bt.Name)
                    {
                        current_panel = (Panel)tmp;

                        /**将之前点击的panel恢复大小**/
                        if (pre_panel != null)
                            pre_panel.Size = new Size(260, 30);
                        
                        /**将当前点击panel保存为pre_panel**/
                        pre_panel = (Panel)tmp;

                        if (ssids[0].wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Disconnected)
                        {
                            current_connect_ssid = null;
                        }

                        else
                        {
                            if (bt_info[0] == current_connect_ssid.SSID && bt_info[1] == current_connect_ssid.dot11DefaultAuthAlgorithm.ToString() && current_connect_ssid.wlanInterface.InterfaceState != Wlan.WlanInterfaceState.Connected)
                            {
                                tmp.Size = new System.Drawing.Size(260, 60);
                                return;
                            }
                        }

                        /**点击连接时定位ssid**/
                        Button bt = new Button();
                        bt.Name = curr_bt.Name;

                        if (ssids[0].wlanInterface.InterfaceState != Wlan.WlanInterfaceState.Connected)
                        {
                            tmp.Size = new Size(260, 90);

                            bt.Location = new Point(180, 60);
                            bt.Text = "连接";
                            bt.Click += new EventHandler(connect_to_wifi);

                            current_checkbox.Text = "自动连接";
                            current_checkbox.Location = new Point(10, 32);

                            /***如果ssid认证方式为open则不添加密码框***/
                            if (!(bt_info[1] == Wlan.Dot11AuthAlgorithm.IEEE80211_Open.ToString()))
                            {
                                current_textbox.Text = "";
                                current_textbox.Location = new Point(10, 62);
                                tmp.Controls.Add(current_textbox);

                                current_key_err.Location = new Point(100, 37);
                                current_key_err.Text = "输入密码错误";
                                tmp.Controls.Add(current_key_err);
                                current_key_err.Hide();
                            }
                            tmp.Controls.Add(current_checkbox);
                            tmp.Controls.Add(bt);
                            break;
                        }
                        else if(ssids[0].wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                        {
                            /****通过配置文件找到当期连接的ssid****/
                            string profilexml = ssids[0].wlanInterface.GetProfileXml(ssids[0].wlanInterface.CurrentConnection.profileName);
                            string[] str = GetProfileInfo(profilexml);
                            /**当前连接的ssid显示为断开连接，其他的ssid显示为连接,注意通过认证方式区分同名ssid**/
                            if(bt_info[0] == str[0] && bt_info[1] == str[1])
                            {
                                tmp.Size = new Size(260, 60);
                                bt.Location = new Point(180, 30);
                                bt.Name = curr_bt.Name;
                                bt.Text = "断开连接";
                                bt.Click += new EventHandler(disconnect_to_wifi);
                                tmp.Controls.Add(bt);
                                break;
                            }
                            else
                            {
                                tmp.Size = new Size(260, 90);

                                bt.Location = new Point(180, 60);
                                bt.Text = "连接";
                                bt.Click += new EventHandler(connect_to_wifi);

                                current_checkbox.Text = "自动连接";
                                current_checkbox.Location = new Point(10, 32);

                                /***如果ssid认证方式为open则不添加密码框***/
                                if (!(bt_info[1] == Wlan.Dot11AuthAlgorithm.IEEE80211_Open.ToString()))
                                {
                                    current_textbox.Text = "";
                                    current_textbox.Location = new Point(10, 62);
                                    tmp.Controls.Add(current_textbox);

                                    current_key_err.Location = new Point(100, 37);
                                    current_key_err.Text = "输入密码错误";
                                    tmp.Controls.Add(current_key_err);
                                    current_key_err.Hide();
                                }
                                tmp.Controls.Add(current_checkbox);
                                tmp.Controls.Add(bt);
                                break;
                            }
                        }
                    }
                }
                tmp = panel.GetNextControl((System.Windows.Forms.Control)tmp, true);
            }
        }

        //连接到wifi
        private void connect_to_wifi(object sender, EventArgs e)
        {
            Button bt = sender as Button;
            string[] str = Regex.Split(bt.Name, "兠凟剅吺唗投斣枓", RegexOptions.IgnoreCase);
            Connection(str[0], str[1], current_textbox.Text);
        }

        //断开连接
        private void disconnect_to_wifi(object sender, EventArgs e)
        {
            current_connect_ssid = null;
            client.Interfaces[0].Disconnect();
            deleteAllProfiles();
            /***断开后刷新wifi面板***/
            Controls.Clear();
            InitializeComponent();
            ScanSSID();
            show_wifi();
        }

        /***通过无线配置文件与ssid配对选择信号最强的ssid连接, 并设为static, 用于软件开启后自动连接wifi***/
        public void AutoConnection()
        {
            try
            {
                if (ssids.Count != 0 && ssids[0].wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected) return;
                else
                {
                    deleteAllProfiles();
                    int maxSignal = -1, bestssid = -1;
                    XmlNode bestNode = null;
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load("profile.xml");
                    if (xmlDoc != null)
                    {
                        XmlNodeList listNodes = xmlDoc.SelectNodes("/ProfileXml/SSID");
                        if (listNodes != null && listNodes.Count != 0)
                        {
                            foreach (XmlNode node in listNodes)
                            {
                                if (ssids.Count != 0)
                                {
                                    for (int i = 0; i < ssids.Count; i++)
                                    {
                                        if (node.ChildNodes[1].InnerText == ssids[i].SSID && node.ChildNodes[2].InnerText == ssids[i].dot11DefaultAuthAlgorithm.ToString() && node.ChildNodes[4].InnerText == "true")
                                        {
                                            if (ssids[i].wlanSignalQuality > maxSignal)
                                            {
                                                maxSignal = ssids[i].wlanSignalQuality;
                                                bestssid = i;
                                                bestNode = node;
                                            }
                                        }
                                    }
                                }
                            }
                            if (bestssid != -1)
                            {
                                current_connect_ssid = ssids[bestssid];
                                current_textbox.Text = bestNode.ChildNodes[3].InnerText;
                                Connection(ssids[bestssid].SSID, ssids[bestssid].dot11DefaultAuthAlgorithm.ToString(), bestNode.ChildNodes[3].InnerText);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                FileStream fs = new FileStream("log.txt", FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(System.DateTime.Now + "     " + ex.Message.ToString());
                sw.Close();
                fs.Close();
            }
        }

        public void Connection(string ssid, string AuthAlgorithm, string key)
        {
            foreach (var item in ssids)
            {
                if (item.SSID == ssid && item.dot11DefaultAuthAlgorithm.ToString() == AuthAlgorithm)
                {
                    /**如果ssid认证方式不为open且输入密码长度不为8**/
                    if (item.dot11DefaultAuthAlgorithm != Wlan.Dot11AuthAlgorithm.IEEE80211_Open && key.Length < 8)
                    {
                        current_key_err.Show();
                        break;
                    }
                    else
                    {
                        /**密码输入错误不记录current_connect_ssid**/
                        current_connect_ssid = item;

                        /*每次连接都更新软件无线配置文件*/
                        addProfile(current_connect_ssid);
                        
                        Thread thread_connection = new Thread(new ThreadStart(MainWindow.wifi.ConnectToSSID));
                        thread_connection.Start();
                        while (!thread_connection.IsAlive)
                            Thread.Sleep(10);
                    }
                }
            }
        }

        public void ConnectToSSID()
        {
            try
            {
                CheckForIllegalCrossThreadCalls = false;

                //将当前点击Panel保存为当前连接Panel
                current_connect_panel = current_panel;

                current_connect_panel.Size = new System.Drawing.Size(260, 60);
                Button bt = new Button();
                bt = (Button)current_connect_panel.GetNextControl((System.Windows.Forms.Control)current_connect_panel, true);
                
                current_connect_panel.Controls.Clear();
                current_connect_panel.Controls.Add(bt);
                Label connecting = new Label();
                connecting.Location = new Point(0, 30);
                connecting.Size = new System.Drawing.Size(260, 30);
                connecting.Text = "正在验证并连接";
                connecting.TextAlign = ContentAlignment.MiddleCenter;
                /**解决"在某个线程上创建的控件不能成为在另一个线程上创建的控件的父级"**/
                if (InvokeRequired)
                {
                    Invoke(new MethodInvoker(delegate { current_connect_panel.Controls.Add(connecting); }));
                }
                current_connect_panel.Controls.Add(connecting);
                
                String auth = "";
                String cipher = "";
                String keytype = "";
                /**判断ssid的认证方式**/
                switch (current_connect_ssid.dot11DefaultAuthAlgorithm)
                {
                    case Wlan.Dot11AuthAlgorithm.IEEE80211_Open:
                        auth = "open";
                        break;
                    case Wlan.Dot11AuthAlgorithm.RSNA:
                        auth = "WPA2PSK";
                        break;
                    case Wlan.Dot11AuthAlgorithm.RSNA_PSK:
                        auth = "WPA2PSK";
                        break;
                    case Wlan.Dot11AuthAlgorithm.WPA:
                        auth = "WPAPSK";
                        break;
                    case Wlan.Dot11AuthAlgorithm.WPA_None:
                        auth = "WPAPSK";
                        break;
                    case Wlan.Dot11AuthAlgorithm.WPA_PSK:
                        auth = "WPAPSK";
                        break;
                }
                /**判断ssid的加密方式**/
                switch (current_connect_ssid.dot11DefaultCipherAlgorithm)
                {
                    case Wlan.Dot11CipherAlgorithm.CCMP:
                        cipher = "AES";
                        keytype = "passPhrase";
                        break;
                    case Wlan.Dot11CipherAlgorithm.TKIP:
                        cipher = "TKIP";
                        keytype = "passPhrase";
                        break;
                    case Wlan.Dot11CipherAlgorithm.None:
                        cipher = "none";
                        keytype = "";
                        break;
                    case Wlan.Dot11CipherAlgorithm.WEP:
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                    case Wlan.Dot11CipherAlgorithm.WEP40:
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                    case Wlan.Dot11CipherAlgorithm.WEP104:
                        cipher = "WEP";
                        keytype = "networkKey";
                        break;
                }
                /***删除系统所有配置文件,防止系统的自动连接对软件功能的影响****/
                deleteAllProfiles();

                string profileName = current_connect_ssid.SSID;
                string mac = StringToHex(profileName);
                string profileXml = "";
                if (auth == "open")
                    profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{2}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><MSM><security><authEncryption><authentication>{3}</authentication><encryption>{4}</encryption><useOneX>false</useOneX></authEncryption></security></MSM></WLANProfile>", profileName, mac, profileName, auth, cipher, keytype);
                else
                    profileXml = string.Format("<?xml version=\"1.0\"?><WLANProfile xmlns=\"http://www.microsoft.com/networking/WLAN/profile/v1\"><name>{0}</name><SSIDConfig><SSID><hex>{1}</hex><name>{2}</name></SSID></SSIDConfig><connectionType>ESS</connectionType><connectionMode>auto</connectionMode><MSM><security><authEncryption><authentication>{3}</authentication><encryption>{4}</encryption><useOneX>false</useOneX></authEncryption><sharedKey><keyType>{5}</keyType><protected>false</protected><keyMaterial>{6}</keyMaterial></sharedKey></security></MSM></WLANProfile>", profileName, mac, profileName, auth, cipher, keytype, current_textbox.Text);
                current_connect_ssid.wlanInterface.SetProfile(Wlan.WlanProfileFlags.AllUser, profileXml, true);
                current_connect_ssid.wlanInterface.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, profileName, 5000);

                /**连接成功,panel恢复初始状态**/
                if (current_connect_ssid.wlanInterface.InterfaceState == Wlan.WlanInterfaceState.Connected)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { Controls.Clear(); }));
                    }
                    Controls.Clear();
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { InitializeComponent(); }));
                    }
                    InitializeComponent();
                    ScanSSID();
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { show_wifi(); }));
                    }
                    show_wifi();
                }
                else
                {
                    /**连接失败后,删除对应的无线配置文件**/
                    XmlDocument XmlDoc = new XmlDocument();
                    XmlDoc.Load("profile.xml");
                    XmlNode root = XmlDoc.SelectSingleNode("ProfileXml");
                    XmlNodeList listNodes = XmlDoc.SelectNodes("/ProfileXml/SSID");
                    if (listNodes.Count != 0)
                    {
                        foreach (XmlNode node in listNodes)
                        {
                            if (node.ChildNodes[1].InnerText == current_connect_ssid.SSID && node.ChildNodes[2].InnerText == current_connect_ssid.dot11DefaultAuthAlgorithm.ToString())
                            {
                                root.RemoveChild(node);
                                XmlDoc.Save("profile.xml");
                                break;
                            }
                        }
                    }

                    current_connect_panel.Size = new System.Drawing.Size(260, 60);
                    bt = (Button)current_connect_panel.GetNextControl((System.Windows.Forms.Control)current_connect_panel, true);
                    current_connect_panel.Controls.Clear();

                    /**解决"在某个线程上创建的控件不能成为在另一个线程上创建的控件的父级"**/
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { current_connect_panel.Controls.Add(bt); }));
                    }
                    current_connect_panel.Controls.Add(bt);

                    Button bt_ret = new Button();
                    bt_ret.Location = new Point(180, 30);
                    bt_ret.Text = "确认";
                    bt_ret.Click += new EventHandler(reShow_wifi);

                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { current_connect_panel.Controls.Add(bt_ret); }));
                    }
                    current_connect_panel.Controls.Add(bt_ret);

                    Label connecting_error = new Label();
                    connecting_error.Location = new Point(0, 30);
                    connecting_error.Size = new System.Drawing.Size(260, 30);
                    connecting_error.Text = "无法完成连接请求";
                    connecting.TextAlign = ContentAlignment.MiddleLeft;
                    
                    if (InvokeRequired)
                    {
                        Invoke(new MethodInvoker(delegate { current_connect_panel.Controls.Add(connecting_error); }));
                    }
                    current_connect_panel.Controls.Add(connecting_error);
                }

                Thread.CurrentThread.Abort();
            }
            catch (Exception ex)
            {
                FileStream fs = new FileStream("log.txt", FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(System.DateTime.Now + "     " + ex.Message.ToString());
                sw.Close();
                fs.Close();
            }
        }

        //获得wifi设备名称
        static string GetStringForSSID(Wlan.Dot11Ssid ssid)
        {
            return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
        }

        private void reShow_wifi(object sender, EventArgs e)
        {
            /**将current_connect_ssid初始化**/
            current_connect_ssid.SSID = "NONE";
            current_connect_ssid.dot11DefaultAuthAlgorithm = Wlan.Dot11AuthAlgorithm.IEEE80211_Open;

            Controls.Clear();
            InitializeComponent();
            ScanSSID();
            show_wifi();
        }

        //将设备名转为mac地址
        static string StringToHex(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = System.Text.Encoding.Default.GetBytes(str);
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(Convert.ToString(byStr[i], 16));
            }
            return (sb.ToString().ToUpper());
        }

        //刷新wifi设备
        private void refresh_wifi(object sender, EventArgs e)
        {
            ssids = null;
            ScanSSID();
            show_wifi();
        }

        /*隐藏面板*/
        private void hide(object sender, EventArgs e)
        {
            Visible = false;
        }

        /**配置文件获得ssid及认证方式**/
        public string[] GetProfileInfo(string profile)
        {
            string[] str = new string[3];
            try
            {
                /***带命名空间的无线配置文件xml解析****/
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(profile);
                XmlNode root = xmlDoc.DocumentElement;
                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ab", "http://www.microsoft.com/networking/WLAN/profile/v1");

                XmlNode ssid = xmlDoc.SelectSingleNode("/ab:WLANProfile/ab:SSIDConfig/ab:SSID/ab:name", nsmgr);
                str[0] = ssid.InnerText;
                XmlNode authentication = xmlDoc.SelectSingleNode("/ab:WLANProfile/ab:MSM/ab:security/ab:authEncryption/ab:authentication", nsmgr);
                switch(authentication.InnerText)
                {
                    case "open":
                        str[1] = "IEEE80211_Open";
                        break;
                    case "WPA2PSK":
                        str[1] = "RSNA_PSK";
                        break;
                    case "WPAPSK":
                        str[1] = "WPA_PSK";
                        break;
                }
                return str;
            }
            catch (Exception ex)
            {
                FileStream fs = new FileStream("log.txt", FileMode.OpenOrCreate);
                StreamWriter sw = new StreamWriter(fs);
                sw.WriteLine(System.DateTime.Now + "     " + ex.Message.ToString());
                sw.Close();
                fs.Close();
                return null;
            }
        }

        private void addProfile(WIFISSID ssid)
        {
            /**添加软件无线配置文件**/
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load("profile.xml");
            if (xmlDoc != null)
            {
                bool hasSame = false, hasDiff = false;
                XmlNodeList listNodes = xmlDoc.SelectNodes("/ProfileXml/SSID");
                XmlElement Newssid = xmlDoc.CreateElement("SSID");
                XmlElement ssidsub_1 = xmlDoc.CreateElement("ProfileName");
                XmlElement ssidsub_2 = xmlDoc.CreateElement("SsidName");
                XmlElement ssidsub_3 = xmlDoc.CreateElement("AuthAlgorithm");
                XmlElement ssidsub_4 = xmlDoc.CreateElement("Key");
                XmlElement ssidsub_5 = xmlDoc.CreateElement("AutoConnection");
                if (listNodes != null && listNodes.Count != 0)
                {
                    /***以ssid和认证方式相同优先判断,防止写入重复,只修改密码***/
                    foreach (XmlNode node in listNodes)
                    {
                        if (node.ChildNodes[1].InnerText == ssid.SSID.ToString() && node.ChildNodes[2].InnerText == ssid.dot11DefaultAuthAlgorithm.ToString())
                        {
                            if(current_textbox.TextLength != 0)
                                node.ChildNodes[3].InnerText = current_textbox.Text;
                            if (current_checkbox.Checked == true)
                                node.ChildNodes[4].InnerText = "true";
                            else
                                node.ChildNodes[4].InnerText = "false";
                            xmlDoc.Save("profile.xml");
                            hasSame = true;
                            break;
                        }
                    }
                    /****若出现同名ssid且认证方式不同,添加新配置文件,文件名+2****/
                    foreach (XmlNode node in listNodes)
                    {
                        if (!hasSame && node.ChildNodes[1].InnerText == ssid.SSID.ToString() && node.ChildNodes[2].InnerText != ssid.dot11DefaultAuthAlgorithm.ToString())
                        {
                            XmlNode root = xmlDoc.SelectSingleNode("ProfileXml");
                            ssidsub_1.InnerText = ssid.SSID.ToString() + " 2";
                            ssidsub_2.InnerText = ssid.SSID.ToString();
                            ssidsub_3.InnerText = ssid.dot11DefaultAuthAlgorithm.ToString();
                            ssidsub_4.InnerText = current_textbox.Text;
                            if (current_checkbox.Checked == true)
                                ssidsub_5.InnerText = "true";
                            else
                                ssidsub_5.InnerText = "false";
                            Newssid.AppendChild(ssidsub_1);
                            Newssid.AppendChild(ssidsub_2);
                            Newssid.AppendChild(ssidsub_3);
                            Newssid.AppendChild(ssidsub_4);
                            Newssid.AppendChild(ssidsub_5);
                            root.AppendChild(Newssid);
                            xmlDoc.Save("profile.xml");
                            hasDiff = true;
                            break;
                        }
                    }
                }
                /**若配置文件为空或者不存在同名ssid,则添加新配置文件**/
                if (listNodes != null && (listNodes.Count == 0 || (!hasSame && !hasDiff)))
                {
                    XmlNode root = xmlDoc.SelectSingleNode("ProfileXml");
                    ssidsub_1.InnerText = ssid.SSID.ToString();
                    ssidsub_2.InnerText = ssid.SSID.ToString();
                    ssidsub_3.InnerText = ssid.dot11DefaultAuthAlgorithm.ToString();
                    ssidsub_4.InnerText = current_textbox.Text;
                    if (current_checkbox.Checked == true)
                        ssidsub_5.InnerText = "true";
                    else
                        ssidsub_5.InnerText = "false";
                    Newssid.AppendChild(ssidsub_1);
                    Newssid.AppendChild(ssidsub_2);
                    Newssid.AppendChild(ssidsub_3);
                    Newssid.AppendChild(ssidsub_4);
                    Newssid.AppendChild(ssidsub_5);
                    root.AppendChild(Newssid);
                    xmlDoc.Save("profile.xml");
                }
            }
        }

        public void deleteAllProfiles()
        {
            if (ssids.Count != 0)
            {
                Wlan.WlanProfileInfo[] profiles = ssids[0].wlanInterface.GetProfiles();
                foreach (var pro in profiles)
                    ssids[0].wlanInterface.DeleteProfile(pro.profileName);
            }
        }
    }
}
