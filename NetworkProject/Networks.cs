using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkProject
{
    public class NetworkPackageData
    {
        public int length, type;
        public byte[] data;
        public const int RDSERVICE_SCREENSENDER = 0;
        public const int RDSERVICE_CONTROLSENDER = 1;
        public const int RDSERVICE_FILETRANSFER = 2;
        public const int RDSERVICE_PASSWORD = - 1;
        public const int RDSERVICE_MAX = 2; // EOF
        public const int MAX_MSGPACKETSIZE_SOFT = 4000;
        public byte[] GetBytes()
        {
            try
            {
                byte[] res = new byte[8 + data.Length];
                Array.Copy(BitConverter.GetBytes(length), 0, res, 0, 4);
                Array.Copy(BitConverter.GetBytes(type), 0, res, 4, 4);
                Array.Copy(data, 0, res, 8, data.Length);
                return res;
            }
            catch
            {
                return null;
            }
        }
    }
    public struct BITMAPINFOHEADER
    {
        public UInt32 biSize;
        public Int32 biWidth;
        public Int32 biHeight;
        public Int16 biPlanes;
        public Int16 biBitCount;
        public UInt32 biCompression;
        public UInt32 biSizeImage;
        public Int32 biXPelsPerMeter;
        public Int32 biYPelsPerMeter;
        public UInt32 biClrUsed;
        public UInt32 biClrImportant;
        public unsafe BITMAPINFOHEADER(byte[] input, int start = 0)
        {
            if (input.Length - start < sizeof(BITMAPINFOHEADER)) throw new Exception();
            biSize = BitConverter.ToUInt32(input, start);
            biWidth = BitConverter.ToInt32(input, start += 4);
            biHeight = BitConverter.ToInt32(input, start += 4);
            biPlanes = BitConverter.ToInt16(input, start += 4);
            biBitCount = BitConverter.ToInt16(input, start += 2);
            biCompression = BitConverter.ToUInt32(input, start += 2);
            biSizeImage = BitConverter.ToUInt32(input, start += 4);
            biXPelsPerMeter = BitConverter.ToInt32(input, start += 4);
            biYPelsPerMeter = BitConverter.ToInt32(input, start += 4);
            biClrUsed = BitConverter.ToUInt32(input, start += 4);
            biClrImportant = BitConverter.ToUInt32(input, start += 4);
        }
    };
    public struct RGBQUAD
    {
        public byte rgbBlue;
        public byte rgbGreen;
        public byte rgbRed;
        public byte rgbReserved;
        public unsafe RGBQUAD(byte[] input, int start = 0)
        {
            if (input.Length - start < sizeof(RGBQUAD)) throw new Exception("in RGBQARD: need length: " + sizeof(RGBQUAD) + ", input length: " + (input.Length - start));
            rgbBlue = input[start++];
            rgbGreen = input[start++];
            rgbRed = input[start++];
            rgbReserved = input[start];
        }
    };
    public struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;
        public unsafe BITMAPINFO(byte[] input, int start = 0)
        {
            if (input.Length - start < sizeof(BITMAPINFOHEADER)) throw new Exception();
            bmiHeader = new BITMAPINFOHEADER(input, start);
        }
    };
    public struct ScreenHdr
    {
        public int type;
        public int id;
        public byte[] data;
	    public const int SCRPKT_BITMAPINFO = 0;
        public const int SCRPKT_BITMAPDATA = 1;
        public const int SCRPKT_BITMAPDATA_COMPRESSED = 2;
        public const int SCRPKT_PADDING = 3;
        public const int SCRPKT_MAXDATA = (NetworkPackageData.MAX_MSGPACKETSIZE_SOFT - 8);
        public ScreenHdr(byte[] input)
        {
            if (input.Length < 8) throw new Exception();
            type = BitConverter.ToInt32(input, 0);
            id = BitConverter.ToInt32(input, 4);
            data = new byte[input.Length - 8];
            Array.Copy(input, 8, data, 0, input.Length - 8);
        }
    };
    public struct ControlHdr
    {
        public int type;
        public const int PADDING = 0;
        public const int MOUSEMOVE = 1;
        public const int MOUSELEFT = 2;
        public const int MOUSEMID = 3;
        public const int MOUSERIGHT = 4;
        public const int KEYBOARD = 5;

        public int id;
        public int args;
        public byte[] data;
        public byte[] GetBytes()
        {
            try
            {
                if (data == null) data = new byte[0];
                byte[] res = new byte[12 + data.Length];
                Array.Copy(BitConverter.GetBytes(type), 0, res, 0, 4);
                Array.Copy(BitConverter.GetBytes(id), 0, res, 4, 4);
                Array.Copy(BitConverter.GetBytes(args), 0, res, 8, 4);
                Array.Copy(data, 0, res, 12, data.Length);
                return res;
            }
            catch
            {
                return null;
            }
        }
    }
    public struct FileTransferHdr
    {
        public const int SEND_REQUEST = 0;
        public const int SEND_RESPONSE = 1;
        public const int SEND_DATA = 2;
        public const int TRANSFER_CANCEL = 3;
        public const int DOWNLOAD_REQUEST = 4;

        public int type;
        public int id;
        public int length;
        public byte[] data;
        public byte[] GetBytes()
        {
            try
            {
                if (data == null) data = new byte[0];
                byte[] res = new byte[12 + data.Length];
                Array.Copy(BitConverter.GetBytes(type), 0, res, 0, 4);
                Array.Copy(BitConverter.GetBytes(id), 0, res, 4, 4);
                Array.Copy(BitConverter.GetBytes(length), 0, res, 8, 4);
                Array.Copy(data, 0, res, 12, data.Length);
                return res;
            }
            catch
            {
                return null;
            }
        }
        public FileTransferHdr(byte[] input)
        {
            if (input.Length < 12) throw new Exception();
            type = BitConverter.ToInt32(input, 0);
            id = BitConverter.ToInt32(input, 4);
            length = BitConverter.ToInt32(input, 8);
            data = new byte[input.Length - 12];
            Array.Copy(input, 12, data, 0, input.Length - 12);
        }
    }
}
