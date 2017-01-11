using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetworkProject
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public const string DLLPATH = @"MiniLZO.dll";
        public static bool socketstart = false;
        
[DllImport(DLLPATH)] static extern unsafe void MiniLZO_AllocBuffer(UInt32 len);
[DllImport(DLLPATH)] static extern unsafe UInt32 MiniLZO_Compress(void *output, void *input, UInt32 in_len);
[DllImport(DLLPATH)] static extern unsafe UInt32 MiniLZO_GetOrigSize(void *input, UInt32 in_len);
[DllImport(DLLPATH)] static extern unsafe int MiniLZO_Decompress(void *output, void *input, UInt32 in_len);


        public MainWindow()
        {
            InitializeComponent();
            ControlHdr hdr = new ControlHdr();
            hdr.data = new byte[0];
            hdr.args = 0;
            hdr.id = 0;
            hdr.type = ControlHdr.PADDING;
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            //MessageBox.Show(pkg.length.ToString());
            pkg.type = NetworkPackageData.RDSERVICE_CONTROLSENDER;
            RawPackage = pkg.GetBytes();
        }

        Socket socket;
        ConnectWindow conwin;
        const int MAX_BUFFER = 8388608;
        byte[] socketbuffer = new byte[MAX_BUFFER], databuffer = new byte[2 * MAX_BUFFER], sendbuffer = new byte[MAX_BUFFER];
        byte[] RawPackage;
        int datastart = 0, dataend = 0;
        NetworkPackageData packagedata;

        private void socketreceivecallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int byteread = socket.EndReceive(ar);
                if (byteread > 0)
                {
                    if (dataend + byteread >= MAX_BUFFER)
                    {
                        int length = dataend - datastart;
                        for (int i = 0; i < length; i++)
                            databuffer[i] = databuffer[datastart + i];
                        datastart = 0;
                        dataend = length;
                    }
                    for (int i = 0; i < byteread; i++)
                        databuffer[dataend++] = socketbuffer[i];
                    for (;;)
                    {
                        if (dataend - datastart < 4) break;
                        packagedata = new NetworkPackageData();
                        packagedata.length = BitConverter.ToInt32(databuffer, datastart) + 8;
                        if (dataend - datastart < packagedata.length) break;
                        packagedata.type = BitConverter.ToInt32(databuffer, datastart + 4);
                        packagedata.length -= 8;
                        packagedata.data = new byte[packagedata.length];
                        //MessageBox.Show(packagedata.length.ToString());
                        Array.Copy(databuffer, datastart + 8, packagedata.data, 0, packagedata.length);
                        for (int i = 0; ; i++ )
                            try
                            {
                                lock (conwin.ReveiveDataQueue)
                                conwin.ReveiveDataQueue.Enqueue(packagedata);
                                break;
                            }
                            catch
                            {
                                //MessageBox.Show("UnexpectedError: " + i);
                            }
                        datastart += 8 + packagedata.length;
                    }
                }
                socket.BeginReceive(socketbuffer, 0, socketbuffer.Length, 0, socketreceivecallback, socket);
            }
            catch(Exception ex)
            {
                if (!socketstart) return;
                socketstart = false;
                MessageBox.Show("Socket 接收错误！数据接收终止。\n错误信息：\n" + ex.Message);
                if (conwin != null && conwin.IsVisible == true)
                    conwin.Close();
            }
        }

        DateTime LastSendTime;
        const int ExpectedSendMS = 100;
        const int defaultdatapkg = 10;
        int lastdatapkg;
        private void socketsendcallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                int bytesend = socket.EndSend(ar);
                int length = 0;
                int ControlQueueCount, MouseMoveQueueCount, FileSendQueueCount;
                lock (conwin.ControlQueue)
                ControlQueueCount = conwin.ControlQueue.Count;
                lock (conwin.ControlQueue)
                MouseMoveQueueCount = conwin.MouseMoveQueue.Count;
                lock (conwin.ControlQueue)
                FileSendQueueCount = conwin.FileSendQueue.Count;
                if (ControlQueueCount == 0 && MouseMoveQueueCount == 0 && FileSendQueueCount == 0)
                {
                    Thread.Sleep(1000 / ConnectWindow.MAX_MOUSEMOVECHECK);
                    socket.BeginSend(RawPackage, 0, RawPackage.Length, 0, socketsendcallback, socket);
                }
                else
                {
                    if (ControlQueueCount > 0)
                    {
                        length = 0;
                        lock (conwin.ControlQueue)
                        for (; conwin.ControlQueue.Count != 0; )
                        {
                            NetworkPackageData pkg;
                            pkg = conwin.ControlQueue.Dequeue();
                            Array.Copy(pkg.GetBytes(), 0, sendbuffer, length, pkg.data.Length + 8);
                            length += 8 + pkg.data.Length;
                        }
                        //MessageBox.Show("length: " + length);
                    }
                    else if (MouseMoveQueueCount > 0)
                    {
                        
                        NetworkPackageData pkg;
                        lock (conwin.MouseMoveQueue)
                        pkg = conwin.MouseMoveQueue.Dequeue();
                        Array.Copy(pkg.GetBytes(), 0, sendbuffer, length, pkg.data.Length + 8);
                        length += 8 + pkg.data.Length;
                    }
                    if (FileSendQueueCount > 0)
                    {
                        if (lastdatapkg < defaultdatapkg)
                            lastdatapkg = defaultdatapkg;
                        int usedms = Convert.ToInt32((DateTime.Now - LastSendTime).TotalMilliseconds);
                        if (usedms == 0) usedms = 1;
                        int pkgs = lastdatapkg * ExpectedSendMS / usedms;
                        if (pkgs < defaultdatapkg)
                            pkgs = defaultdatapkg;
                        lastdatapkg = 0;
                        lock (conwin.FileSendQueue)
                        for (; lastdatapkg < pkgs && conwin.FileSendQueue.Count > 0; )
                        {
                            lastdatapkg++;
                            NetworkPackageData pkg;
                            pkg = conwin.FileSendQueue.Dequeue();
                            Array.Copy(pkg.GetBytes(), 0, sendbuffer, length, pkg.data.Length + 8);
                            length += 8 + pkg.data.Length;
                        }
                    }
                    LastSendTime = DateTime.Now;
                    socket.BeginSend(sendbuffer, 0, length, 0, socketsendcallback, socket);
                }
            }
            catch (Exception ex)
            {
                if (!socketstart) return;
                MessageBox.Show("Socket 发送错误！尝试稍后发送。\n错误信息：\n" + ex.Message);
                Thread.Sleep(1000);
                try
                {
                    socket.BeginSend(RawPackage, 0, RawPackage.Length, 0, socketsendcallback, socket);
                }
                catch
                {
                    if (!socketstart) return;
                    socketstart = false;
                    MessageBox.Show("Socket 再次发送错误！发送数据终止。\n错误信息：\n" + ex.Message);
                    if (conwin != null && conwin.IsVisible == true)
                        conwin.Close();
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(IPTextBox.Text), Convert.ToInt32(PortTextBox.Text));
                socket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipe);
                if (socket.Connected)
                {
                    //MessageBox.Show("Connected");
                    socketstart = true;
                    conwin = new ConnectWindow();
                    socket.BeginReceive(socketbuffer, 0, socketbuffer.Length, 0, socketreceivecallback, socket);
                    NetworkPackageData pkg = new NetworkPackageData();
                    pkg.type = NetworkPackageData.RDSERVICE_PASSWORD;
                    pkg.length = PasswordPasswordtBox.Password.Length;
                    pkg.data = Encoding.Default.GetBytes(PasswordPasswordtBox.Password);
                    byte[] data = pkg.GetBytes();
                    LastSendTime = DateTime.Now;
                    socket.BeginSend(data, 0, data.Length, 0, socketsendcallback, socket);
                }
                else
                {
                    MessageBox.Show("连接未建立！");
                }
                conwin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("建立连接错误！\n错误信息：\n" + ex.Message);
            }
            socketstart = false;
            socket.Dispose();
        }
    }
}
