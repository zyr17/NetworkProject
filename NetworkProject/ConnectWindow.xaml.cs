using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetworkProject
{
    /// <summary>
    /// ConnectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConnectWindow : Window
    {
        public const string DLLPATH = MainWindow.DLLPATH;

        [DllImport(DLLPATH)]
        static extern unsafe void MiniLZO_AllocBuffer(UInt32 len);
        [DllImport(DLLPATH)]
        static extern unsafe UInt32 MiniLZO_Compress(void* output, void* input, UInt32 in_len);
        [DllImport(DLLPATH)]
        static extern unsafe UInt32 MiniLZO_GetOrigSize(void* input, UInt32 in_len);
        [DllImport(DLLPATH)]
        static extern unsafe int MiniLZO_Decompress(void* output, void* input, UInt32 in_len);

        public const int MAX_FRAPS = 30, MAX_MOUSEMOVECHECK = 20;
        int ImageStride, ImageWidth, ImageHeight, ImagePerPixel, PackageReceived = 0;
        byte[] ImageByte, ImageByteReversed;
        WriteableBitmap wbitmap;
        FileStream fs;
        DispatcherTimer CheckDataTimer = new DispatcherTimer(DispatcherPriority.Send), MouseMoveTimer = new DispatcherTimer(), FileProgressTimer = new DispatcherTimer(DispatcherPriority.Send);

        public Queue<NetworkPackageData> ReveiveDataQueue, MouseMoveQueue, ControlQueue, FileSendQueue;

        public int FrapsCalcNum;
        public ConnectWindow()
        {
            InitializeComponent();
            ReveiveDataQueue = new Queue<NetworkPackageData>();
            MouseMoveQueue = new Queue<NetworkPackageData>();
            ControlQueue = new Queue<NetworkPackageData>();
            FileSendQueue = new Queue<NetworkPackageData>();
            CheckDataTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / MAX_FRAPS);
            MouseMoveTimer.Interval = new TimeSpan(0, 0, 0, 0, 1000 / MAX_MOUSEMOVECHECK);
            FileProgressTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            CheckDataTimer.Tick += new EventHandler(DataCheckHandler);
            MouseMoveTimer.Tick += new EventHandler(MouseMoveCheckHandler);
            MouseMoveTimer.Tick += new EventHandler(FileProgressCheckHandler);
            CheckDataTimer.Start();
            MouseMoveTimer.Start();
        }
        bool DataCheckLocker = false;
        private bool INFO_DONE;


        void DataCheckHandler(object sender, EventArgs e)
        {
            this.Topmost = false;
            if (MainWindow.socketstart == false)
            {
                this.Close();
                return;
            }
            if (DataCheckLocker) return;
            DataCheckLocker = true;
            FrapsCalcNum++;
            lock (ReveiveDataQueue)
            while (ReveiveDataQueue.Count > 0)
            {
                NetworkPackageData pkg = null;
                //for (; pkg == null && ReveiveDataQueue.Count > 0; )
                    pkg = ReveiveDataQueue.Dequeue();
                //MessageBox.Show(pkg.length.ToString() + " " + pkg.data.Length);
                PackageReceived++;


                //screen pkg
                if (pkg.type == NetworkPackageData.RDSERVICE_SCREENSENDER)
                {
                    ScreenPackageChecker(pkg.data);
                }
                else if (pkg.type == NetworkPackageData.RDSERVICE_FILETRANSFER)
                {
                    //MessageBox.Show("File Package");
                    FilePackageChecker(pkg.data);
                }
                else
                {
                    MessageBox.Show("Unknown package type: " + pkg.type);
                    throw new Exception();
                }
            }
            ImageByteUpdate();
            DataCheckLocker = false;
        }
        void ImageByteUpdate()
        {
            
            try
            {
                for (int i = 0; i < ImageHeight; i++)
                {
                    for (int j = 0; j < ImageStride; j++)
                        ImageByteReversed[i * ImageStride + j] = ImageByte[(ImageHeight - 1 - i) * ImageStride + j];
                }

                wbitmap.WritePixels(new Int32Rect(0, 0, ImageWidth, ImageHeight), ImageByteReversed, ImageStride, 0);
            }
            catch
            {

            }
        }

        string ReceiveFileName;
        private void FilePackageChecker(byte[] input)
        {
            FileTransferHdr hdr = new FileTransferHdr(input);
            switch (hdr.type)
            {
                case FileTransferHdr.SEND_REQUEST:
                    string filename = Encoding.Default.GetString(hdr.data);
                    MessageBox.Show("对方同意下载文件。\n\n文件名：" + filename + "\n文件大小：" + hdr.id + "  字节\n\n请选择文件保存位置");
                    SaveFileDialog SaveDialog = new SaveFileDialog();
                    string suffix = Regex.Match(filename,@"(?<=\.)[^\.]*$").Value;
                    SaveDialog.Filter = suffix + " files (*." + suffix + ")|*." + suffix + "|All files (*.*)|*.*";
                    SaveDialog.FilterIndex = 0;
                    SaveDialog.FileName = filename;
                    FileTransferHdr newhdr = new FileTransferHdr();
                    newhdr.type = FileTransferHdr.SEND_RESPONSE;
                    newhdr.id = 0;
                    newhdr.length = 0;
                    newhdr.data = new byte[0];
                    if (SaveDialog.ShowDialog() == true)
                    {
                        ReceiveFileName = SaveDialog.FileName;
                        fs = new FileStream(SaveDialog.FileName, FileMode.Create);
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            UpperBorder.Width = 600;
                            FileStatusGrid.Visibility = System.Windows.Visibility.Visible;
                            FileProgressBar.Maximum = hdr.id;
                            FilePackageCounter = FileProgressBar.Value = 0;
                        }));
                        FileProgressTimer.Start();
                        newhdr.id = 1;
                    }
                    NetworkPackageData pkg = new NetworkPackageData();
                    pkg.data = newhdr.GetBytes();
                    pkg.length = pkg.data.Length;
                    pkg.type = NetworkPackageData.RDSERVICE_FILETRANSFER;
                    lock (FileSendQueue)
                    {
                        FileSendQueue.Clear();
                        FileSendQueue.Enqueue(pkg);
                    }
                    break;
                case FileTransferHdr.SEND_RESPONSE:
                    if (hdr.id == 0)
                    {
                        ShowMessageBoxAsync("文件上传被拒绝！");
                        fs.Dispose();
                        return;
                    }
                    else
                    {
                        ShowMessageBoxAsync("对方接受文件上传请求，开始上传。");
                        FileTransferAsync async = new FileTransferAsync(FileUploadMain);
                        async.BeginInvoke(FileTransferEnd, async);
                    }
                    break;
                case FileTransferHdr.SEND_DATA:
                    if (fs == null || fs.CanWrite == false) break;
                    lock (fs)
                    {
                        fs.Write(hdr.data, 0, hdr.length);
                        FilePackageCounter += hdr.length;
                    }
                    if (FileProgressBar.Maximum - FilePackageCounter < 1)
                    {
                        ShowMessageBoxAsync("文件下载完成！");
                        FileProgressTimer.Stop();
                        this.Dispatcher.Invoke(new Action(delegate
                        {
                            UpperBorder.Width = 330;
                            FileStatusGrid.Visibility = System.Windows.Visibility.Hidden;
                        }));
                        fs.Dispose();
                        ReceiveFileName = null;
                    }
                    break;
                case FileTransferHdr.TRANSFER_CANCEL:
                    MessageBox.Show("Unexpected Transfer Cancel Received!");
                    throw new Exception();
                    break;
                case FileTransferHdr.DOWNLOAD_REQUEST:
                    MessageBox.Show("Unexpected Download Request Received!");
                    throw new Exception();
                    break;
                default:
                    MessageBox.Show("Unknown FileTransferHdr type: " + hdr.type);
                    throw new Exception();
            }

        }
        private void ScreenPackageChecker(byte[] p)
        {
            ScreenHdr Hdr = new ScreenHdr(p);
            switch (Hdr.type)
            {
                case ScreenHdr.SCRPKT_PADDING:
                    //MessageBox.Show("PADDING package");
                    break;
                case ScreenHdr.SCRPKT_BITMAPINFO:
                    //MessageBox.Show("INFO package");
                    BITMAPINFO info = new BITMAPINFO(Hdr.data, 0);
                    ImageWidth = info.bmiHeader.biWidth;
                    ImageHeight = info.bmiHeader.biHeight;
                    ImagePerPixel = info.bmiHeader.biBitCount / 8;
                    ImageStride = (((info.bmiHeader.biWidth * info.bmiHeader.biBitCount + 7) / 8) + 3) / 4 * 4;
                    ImageByte = new byte[ImageStride * ImageHeight];
                    ImageByteReversed = new byte[ImageStride * ImageHeight];
                    //MessageBox.Show(ImageByte.Length.ToString());
                    wbitmap = new WriteableBitmap(ImageWidth, ImageHeight, 96, 96, PixelFormats.Bgr555, null);
                    ShowImage.Source = wbitmap;
                    INFO_DONE = true;
                    break;
                case ScreenHdr.SCRPKT_BITMAPDATA:
                    //MessageBox.Show("DATA package");
                    if (!INFO_DONE) break;
                    Array.Copy(Hdr.data, 0, ImageByte, Hdr.id * ScreenHdr.SCRPKT_MAXDATA, Hdr.data.Length);
                    break;
                case ScreenHdr.SCRPKT_BITMAPDATA_COMPRESSED:
                    //MessageBox.Show("CMPDATA package");
                    if (!INFO_DONE) break;
                    byte[] output;
                    unsafe
                    {
                        UInt32 length;
                        fixed (byte* @pdata = Hdr.data)
                        {
                            length = MiniLZO_GetOrigSize((void*)pdata, (UInt32)Hdr.data.Length);
                            MiniLZO_AllocBuffer(length);
                            output = new byte[length];
                            fixed (byte* @pout = output)
                            {
                                MiniLZO_Decompress((void*)pout, (void*)pdata, (UInt32)Hdr.data.Length);
                            }
                        }
                    }
                    Array.Copy(output, 0, ImageByte, Hdr.id * ScreenHdr.SCRPKT_MAXDATA, output.Length);
                    break;
                default:
                    MessageBox.Show("Unknown ScreenHdr type: " + Hdr.type);
                    throw new Exception();
            }
        }

        void Change_Window_State()
        {
            if (this.WindowStyle != System.Windows.WindowStyle.None)
            {
                this.Topmost = true;
                this.WindowStyle = System.Windows.WindowStyle.None;
                this.WindowState = System.Windows.WindowState.Normal;
                this.WindowState = System.Windows.WindowState.Maximized;
                ChangeBorderPos(false);
                FullScreenButton.Content = "取消全屏";
            }
            else
            {
                this.WindowState = System.Windows.WindowState.Normal;
                this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
                ChangeBorderPos(true);
                FullScreenButton.Content = "全屏";
            }
        }
        private void FullScreenButtonClick(object sender, RoutedEventArgs e)
        {
            Change_Window_State();
        }
        void AddKeyEventPackage(Key e, int id)
        {
            ControlHdr hdr = new ControlHdr();
            hdr.type = ControlHdr.KEYBOARD;
            hdr.args = 1;
            hdr.id = id;
            hdr.data = new byte[1];
            hdr.data[0] = Convert.ToByte(System.Windows.Input.KeyInterop.VirtualKeyFromKey(e));
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            pkg.type = NetworkPackageData.RDSERVICE_CONTROLSENDER;
            lock(ControlQueue)
            ControlQueue.Enqueue(pkg);
        }
        /*HashSet<byte> KeyPressSet = new HashSet<byte>();*/
        private void ConnectWindow_KeyDown(object sender, KeyEventArgs e)
        {
            //UpButton.Content = Convert.ToInt32(System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key)).ToString();
            /*KeyPressSet.Add(Convert.ToByte(System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key)));*/
            AddKeyEventPackage(e.Key, 1);
        }

        private void ConnectWindow_KeyUp(object sender, KeyEventArgs e)
        {
            //UpButton.Content = Convert.ToInt32(e.Key).ToString();
            /*
            ControlHdr hdr = new ControlHdr();
            hdr.type = ControlHdr.KEYBOARD;
            hdr.id = 0;
            hdr.args = KeyPressSet.Count;
            hdr.data = new byte[hdr.args];
            int i = 0;
            foreach (var k in KeyPressSet)
                hdr.data[i++] = k;
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            pkg.type = NetworkPackageData.RDSERVICE_CONTROLSENDER;
            ControlQueue.Enqueue(pkg);
            KeyPressSet.Remove(Convert.ToByte(System.Windows.Input.KeyInterop.VirtualKeyFromKey(e.Key)));
             */
            AddKeyEventPackage(e.Key, 0);
        }

        int GetControlMouseType(MouseButton e)
        {
            return e == MouseButton.Left ? ControlHdr.MOUSELEFT :
                   e == MouseButton.Middle ? ControlHdr.MOUSEMID :
                   e == MouseButton.Right ? ControlHdr.MOUSERIGHT : -1;
        }
        void AddMouseEventPackage(MouseButton e, int id)
        {
            ControlHdr hdr = new ControlHdr();
            hdr.type = GetControlMouseType(e);
            if (hdr.type == -1) return;
            hdr.args = 0;
            hdr.id = id;
            hdr.data = new byte[0];
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            pkg.type = NetworkPackageData.RDSERVICE_CONTROLSENDER;
            lock (ControlQueue)
            ControlQueue.Enqueue(pkg);
        }
        int MousePosX = 0, MousePosY = 0;
        int LastMousePosX = 0, LastMousePosY = 0;
        void MouseMoveCheckHandler(object sender, EventArgs e)
        {
            if (MousePosX < 0 || MousePosX >= ImageWidth) return;
            if (MousePosY < 0 || MousePosY >= ImageHeight) return;
            if (LastMousePosX == MousePosX && LastMousePosY == MousePosY) return;
            LastMousePosX = MousePosX;
            LastMousePosY = MousePosY;
            if (MouseMoveQueue.Count > 0)
                MouseMoveQueue.Clear();
            lock (MouseMoveQueue)
            MouseMoveQueue.Enqueue(GetMouseMovePackage());
        }
        private void ShowImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            lock (ControlQueue)
            ControlQueue.Enqueue(GetMouseMovePackage());
            AddMouseEventPackage(e.ChangedButton, 1);
        }

        private void ShowImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            lock (ControlQueue)
            ControlQueue.Enqueue(GetMouseMovePackage());
            AddMouseEventPackage(e.ChangedButton, 0);
        }

        private void ShowImage_MouseMove(object sender, MouseEventArgs e)
        {
            var pos = e.GetPosition(ShowImage);
            MousePosX = Convert.ToInt32(pos.X);
            MousePosY = Convert.ToInt32(pos.Y);
        }
        private void ShowImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }
        NetworkPackageData GetMouseMovePackage()
        {
            ControlHdr hdr = new ControlHdr();
            hdr.type = ControlHdr.MOUSEMOVE;
            hdr.id = 0;
            hdr.args = 8;
            hdr.data = new byte[8];
            Array.Copy(BitConverter.GetBytes(MousePosX), 0, hdr.data, 0, 4);
            Array.Copy(BitConverter.GetBytes(MousePosY), 0, hdr.data, 4, 4);
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            pkg.type = NetworkPackageData.RDSERVICE_CONTROLSENDER;
            return pkg;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CheckDataTimer.Stop();
            MouseMoveTimer.Stop();
            if (fs != null) fs.Dispose();
        }

        private void Border_MouseEnter(object sender, MouseEventArgs e)
        {
            ChangeBorderPos(true);
        }

        private void ChangeBorderPos(bool show)
        {
            if (this.WindowStyle != System.Windows.WindowStyle.None)
                show = true;
            if (show) UpperBorder.Height = 30;
            else UpperBorder.Height = 4;
        }

        private void Border_MouseLeave(object sender, MouseEventArgs e)
        {
            ChangeBorderPos(false);
        }

        double FilePackageCounter;
        void FileUploadMain()
        {
            try
            {
                const int DataLengthPerPack = NetworkPackageData.MAX_MSGPACKETSIZE_SOFT - 8 - 12;
                const int PkgsAddPerTime = 200;
                const int MaxQueueLength = 1000;
                lock (FileSendQueue)
                    FileSendQueue.Clear();
                this.Dispatcher.Invoke(new Action(delegate
                {
                    UpperBorder.Width = 600;
                    FileStatusGrid.Visibility = System.Windows.Visibility.Visible;
                    FileProgressBar.Maximum = (fs.Length - 1) / DataLengthPerPack + 1;
                    FilePackageCounter = FileProgressBar.Value = 0;
                }));
                FileProgressTimer.Start();
                for (; ; )
                {
                    if (fs.Position == fs.Length) break;
                    int nowlen = 0;
                    lock (FileSendQueue)
                        nowlen = FileSendQueue.Count;
                    if (nowlen >= MaxQueueLength)
                        Thread.Sleep(100);
                    else
                    {
                        for (int i = 0; i < PkgsAddPerTime; i++)
                        {
                            FilePackageCounter++;
                            if (fs.Length == fs.Position) break;
                            FileTransferHdr hdr = new FileTransferHdr();
                            hdr.type = FileTransferHdr.SEND_DATA;
                            hdr.length = Convert.ToInt32(fs.Length - fs.Position);
                            if (hdr.length > DataLengthPerPack) hdr.length = DataLengthPerPack;
                            hdr.data = new byte[hdr.length];
                            fs.Read(hdr.data, 0, hdr.length);
                            NetworkPackageData pkg = new NetworkPackageData();
                            pkg.type = NetworkPackageData.RDSERVICE_FILETRANSFER;
                            pkg.data = hdr.GetBytes();
                            pkg.length = pkg.data.Length;
                            lock (FileSendQueue)
                            FileSendQueue.Enqueue(pkg);
                        }
                    }
                }
                for (; ; )
                {
                    lock (FileSendQueue)
                        if (FileSendQueue.Count == 0) break;
                    Thread.Sleep(200);
                }
                ShowMessageBoxAsync("文件上传完成！");
                FileProgressTimer.Stop();
                this.Dispatcher.Invoke(new Action(delegate
                {
                    UpperBorder.Width = 330;
                    FileStatusGrid.Visibility = System.Windows.Visibility.Hidden;
                }));
                fs.Dispose();
            }
            catch (Exception ex)
            {
                if (fs == null || fs.CanRead == false) return;
                if (!MainWindow.socketstart) return;
                MessageBox.Show("in FileUploadMain:\n" + ex.Message);
                throw new Exception();
                UpperBorder.Width = 330;
                FileStatusGrid.Visibility = System.Windows.Visibility.Hidden;
                FileProgressTimer.Stop();
            }
        }

        void FileProgressCheckHandler(object sender, EventArgs e)
        {
            FileProgressBar.Value = FilePackageCounter;
        }


        void FileTransferEnd(IAsyncResult ar)
        {
            FileTransferAsync async = (FileTransferAsync)ar.AsyncState;
            async.EndInvoke(ar);
        }

        private delegate void FileTransferAsync();

        private void UploadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog FileSelect = new OpenFileDialog();
            FileSelect.Filter = "All files (*.*)|*.*";
            FileSelect.FilterIndex = 0;
            if (FileSelect.ShowDialog() == true)
            {
                if (File.Exists(FileSelect.FileName))
                {
                    //MessageBox.Show(FileSelect.FileName);
                    try
                    {
                        fs = new FileStream(FileSelect.FileName, FileMode.Open);
                        FileTransferHdr hdr = new FileTransferHdr();
                        hdr.type = FileTransferHdr.SEND_REQUEST;
                        if (fs.Length >= 1L << 31)
                        {
                            ShowMessageBoxAsync("文件过大！请选择2GB以下的文件。");
                            return;
                        }
                        hdr.id = Convert.ToInt32(fs.Length);
                        hdr.data = Encoding.Default.GetBytes(FileSelect.SafeFileName);
                        hdr.length = hdr.data.Length;
                        NetworkPackageData pkg = new NetworkPackageData();
                        pkg.type = NetworkPackageData.RDSERVICE_FILETRANSFER;
                        pkg.data = hdr.GetBytes();
                        pkg.length = pkg.data.Length;
                        lock (ControlQueue)
                        ControlQueue.Enqueue(pkg);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                }
            }
        }

        void ShowMessageBoxMain(string Input)
        {
            MessageBox.Show(Input);
        }

        void ShowMessageBoxEnd(IAsyncResult ar)
        {
            ShowMessageBoxDelegate async = (ShowMessageBoxDelegate)ar.AsyncState;
            async.EndInvoke(ar);
        }

        delegate void ShowMessageBoxDelegate(string Input);

        void ShowMessageBoxAsync(string Input)
        {
            ShowMessageBoxDelegate async = new ShowMessageBoxDelegate(ShowMessageBoxMain);
            async.BeginInvoke(Input, ShowMessageBoxEnd, async);
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            lock (FileSendQueue)
            {
                if (MessageBox.Show("你真的要取消文件传输吗？", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    try
                    {
                        FileSendQueue.Clear();
                        lock (fs)
                        {
                            string delfilename = null;
                            if (fs.CanWrite) delfilename = ReceiveFileName;
                            fs.Dispose();
                            fs = null;
                            try
                            {
                                File.Delete(delfilename);
                            }
                            catch
                            {

                            }
                        }
                        FileTransferHdr hdr = new FileTransferHdr();
                        hdr.type = FileTransferHdr.TRANSFER_CANCEL;
                        hdr.id = hdr.length = 0;
                        hdr.data = new byte[0];
                        NetworkPackageData pkg = new NetworkPackageData();
                        pkg.data = hdr.GetBytes();
                        pkg.length = pkg.data.Length;
                        pkg.type = NetworkPackageData.RDSERVICE_FILETRANSFER;
                        FileSendQueue.Enqueue(pkg);
                        FileProgressTimer.Stop();
                        UpperBorder.Width = 330;
                        FileStatusGrid.Visibility = System.Windows.Visibility.Hidden;
                        ShowMessageBoxAsync("已取消");
                    }
                    catch
                    {
                        throw new Exception();
                    }
                }
            }
        }

        private void DownloadButtonClick(object sender, RoutedEventArgs e)
        {
            FileTransferHdr hdr = new FileTransferHdr();
            hdr.type = FileTransferHdr.DOWNLOAD_REQUEST;
            hdr.id = hdr.length = 0;
            hdr.data = new byte[0];
            NetworkPackageData pkg = new NetworkPackageData();
            pkg.data = hdr.GetBytes();
            pkg.length = pkg.data.Length;
            pkg.type = NetworkPackageData.RDSERVICE_FILETRANSFER;
            lock (FileSendQueue)
            {
                FileSendQueue.Clear();
                FileSendQueue.Enqueue(pkg);
            }
            ShowMessageBoxAsync("下载文件请求已发送");
        }

    }
}
