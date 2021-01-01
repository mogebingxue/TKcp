using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets.Kcp;
using System.Buffers;

namespace System.Net.Sockets.TKcp
{
    class TKcpHandle : IKcpCallback
    {
        /// <summary>
        /// 发送的UDP
        /// </summary>
        public Socket socket;
        /// <summary>
        /// 目标IPEndPoint,DestinationIPEndPoint
        /// </summary>
        public IPEndPoint dipep = null;
        /// <summary>
        /// output
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="avalidLength"></param>
        public void Output(IMemoryOwner<byte> buffer, int avalidLength) {
            if (socket != null) {
                socket.SendTo(buffer.Memory.Span.ToArray(), dipep);
                //Console.WriteLine("UDP发送数据！");
            }
        }
    }
}
