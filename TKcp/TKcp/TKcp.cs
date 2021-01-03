using System;
using System.Net;
using System.Threading;
using System.Net.Sockets.Kcp;
using System.Buffers;

namespace System.Net.Sockets.TKcp
{
    public class TKcp
    {
        /// <summary>
        /// 接受消息的IPEndPoint
        /// </summary>
        IPEndPoint iPEndPoint;
        /// <summary>
        /// Kcp会话
        /// </summary>
        uint conv = 1234;

        TKcpHandle handle;
        Kcp.Kcp kcp;
        Socket socket;


        /// <summary>
        /// 数据源地址和端口号
        /// </summary>
        public IPEndPoint remote;
        /// <summary>
        /// 数据接受缓冲区
        /// </summary>
        byte[] recvBuffer;
        #region 构造函数
        public TKcp(IPAddress ip, int port) {
            this.iPEndPoint = new IPEndPoint(ip, port);
            this.StartTKcp();
        }

        public TKcp(IPAddress ip, int port, uint conv) {
            this.iPEndPoint = new IPEndPoint(ip, port);
            this.conv = conv;
            this.StartTKcp();
        }

        public TKcp(IPEndPoint iPEndPoint) {
            this.iPEndPoint = iPEndPoint;
            this.StartTKcp();
        }

        public TKcp(IPEndPoint iPEndPoint, uint conv) {
            this.iPEndPoint = iPEndPoint;
            this.conv = conv;
            this.StartTKcp();
        }
        #endregion

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes">发送的数据</param>
        /// <param name="iPEndPoint">目标端口</param>
        public void Send(Span<byte> bytes, IPEndPoint dipep) {
            Span<byte> temp = TKcpUtility.Encode(bytes, dipep);
            kcp.Send(temp);
            handle.Out = buffer => {
                int offset = buffer.Span.Length;
                Span<byte> temp = buffer.Span;
                
                while (offset > 0) {
                    Console.WriteLine(offset);
                    var (res, dipep) = TKcpUtility.Decode(temp, ref offset);
                    socket.SendTo(res, dipep);
                    Console.WriteLine("UDP发送数据！" + res.Length+" " + iPEndPoint + " TO " + dipep);
                }
            };


            //Console.WriteLine("应用层发送数据！");
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <returns>接收到的数据</returns>
        public int Receive(Span<byte> buffer) {
            var (temp, avalidSzie) = kcp.TryRecv();
            if (avalidSzie > 0) {
                temp.Memory.Span.Slice(0, avalidSzie).CopyTo(buffer);
                Console.WriteLine("应用层接收数据！");
                return avalidSzie;
            }
            return -1;
        }

        /// <summary>
        /// 启动TKcp
        /// </summary>
        void StartTKcp() {
            //初始化UDP
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(iPEndPoint);
            //初始化Kcp
            handle = new TKcpHandle();

            kcp = new Kcp.Kcp(conv, handle);
            kcp.NoDelay(1, 10, 2, 1);//fast
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            Console.WriteLine("启动TKcp");
            //创建无参的线程
            Thread TKcpUpdata = new Thread(new ThreadStart(this.TKcpUpdata));
            //调用Start方法执行线程
            TKcpUpdata.Start();

        }

        /// <summary>
        /// TKcp的更新操作，负责自动input
        /// </summary>
        void TKcpUpdata() {

            EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
            while (true) {
                kcp.Update(DateTime.UtcNow);
                if (socket.ReceiveBufferSize > 0) {
                    recvBuffer = new byte[socket.ReceiveBufferSize];
                    socket.ReceiveFrom(recvBuffer, ref remote);
                    //Console.WriteLine("UDP接收数据！"+System.Text.Encoding.UTF8.GetString(recvBuffer)+iPEndPoint+" FROM "+remote);
                    kcp.Input(recvBuffer);
                    this.remote = (IPEndPoint)remote;
                    //Console.WriteLine(this.remote.Port);
                    recvBuffer = null;
                }


            }
        }
    }
}
