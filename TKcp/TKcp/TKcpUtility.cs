using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Sockets.TKcp
{
    public class TKcpUtility
    {
        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static Span<byte> Encode(Span<byte> bytes,IPEndPoint ipdp) {
            
            short len = (short)bytes.Length;
            Span<byte> res = new byte[bytes.Length + 8];
            bytes.Slice(0, len).CopyTo(res);
            
            //组装2字节的长度信息
            res[len] = (byte)(len % 256);
            res[len+1] = (byte)(len / 256);
            //组装两字节端口号
            res[len+2] = (byte)(ipdp.Port % 256);
            res[len + 3] = (byte)(ipdp.Port / 256);
            //组装四字节IP地址
            string[] s = ipdp.Address.ToString().Split(".");
            res[len + 4] = (byte)short.Parse(s[0]);
            res[len + 5] = (byte)short.Parse(s[1]);
            res[len + 6] = (byte)short.Parse(s[2]);
            res[len + 7] = (byte)short.Parse(s[3]);

            return res;
        }
        /// <summary>
        /// 解码
        /// </summary>
        /// <returns></returns>
        public static (byte[], IPEndPoint) Decode(Span<byte> bytes,ref int offset) {
            //剩余必须大于8字节
            if (offset - 8 <0) {
                return (null, null);
            }
            //解析目标地址
            IPAddress ip = IPAddress.Parse(
                bytes[offset - 4].ToString()+"."+
                bytes[offset - 3].ToString() + "." +
                bytes[offset - 2].ToString() + "." +
                bytes[offset - 1].ToString());
            //解析端口号
            int port = ((bytes[offset - 5] << 8) | bytes[offset - 6]);
            IPEndPoint iPEndPoint = new IPEndPoint(ip, port);
            //解析长度
            int len = ((bytes[offset - 7] << 8) | bytes[offset-8]);
            
            len += 24;
            offset -= 8;
            byte[] res = new byte[len];
            bytes.Slice(offset - len, len).CopyTo(res);
            offset -= len;
            return (res, iPEndPoint);
        }
    }
}
