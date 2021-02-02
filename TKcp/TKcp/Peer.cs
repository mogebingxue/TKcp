using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.Kcp;

namespace TKcp
{

    public class Peer
    {
        /// <summary>
        /// 本地的 socket
        /// </summary>
        public Socket LocalSocket;
        /// <summary>
        /// 远端的EndPoint
        /// </summary>
        public EndPoint Remote;
        /// <summary>
        /// 连接号
        /// </summary>
        public uint conv;

        public long LastPingTime;
        Handle handle;
        public Kcp kcp;

        public Model model;

        public int TimeoutTime = 0;

        public enum Model
        {
            FAST = 0,
            NORMAL = 1
        }

        /// <summary>
        /// 应用层接收消息之后的回调，在应用层实现connection的时候要自己加上他的实现
        /// </summary>
        public Action<uint, byte[], int> ReceiveHandle;
        /// <summary>
        /// 连接请求的回调，在这里要实现回传同意连接和连接号给客户端，应用层也可以有自己的附加实现
        /// </summary>
        public Action<byte[]> ConnectHandle;
        /// <summary>
        /// 接收连接请求回调，客户端收到服务端的接收连接请求，之后的回调
        /// </summary>
        public Action<byte[], int> AcceptHandle;
        /// <summary>
        /// 断开连接请求回调，服务端断开一个连接，之后的回调
        /// </summary>
        public Action<uint> DisconnectHandle;
        /// <summary>
        /// 客户端连接超时的回调
        /// </summary>
        public Action TimeoutHandle;

        #region 构造函数
        public Peer(Socket socket, uint conv, EndPoint remote, Model model = 0) {
            this.LocalSocket = socket;
            this.conv = conv;
            this.Remote = remote;
            this.model = model;

            LastPingTime = GetTimeStamp();
            this.InitPeer();
        }
        #endregion

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public long GetTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// 初始化Kcp
        /// </summary>
        public void InitKcp() {
            //当这个Peer曾被使用过时，先释放它的Kcp，在重设
            if (kcp != null) {
                kcp.Dispose();
            }

            //初始化Kcp
            handle = new Handle(LocalSocket, Remote);
            kcp = new Kcp(conv, handle);
            if (model == Model.FAST) {
                kcp.NoDelay(1, 10, 2, 1);//fast
            }
            else {
                kcp.NoDelay(0, 40, 0, 0);//normal
            }
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

        }


        /// <summary>
        /// 初始化Peer
        /// </summary>
        void InitPeer() {

            ConnectHandle += OnConnect;
            TimeoutHandle += OnTimeout;
            AcceptHandle += OnAccept;
        }



        /// <summary>
        /// 给客户端发送连接号
        /// </summary>
        /// <param name="conv"></param>
        /// <param name="length"></param>
        private void OnConnect(byte[] conv) {
            byte[] sendBytes = new byte[8];
            //1代表是同意连接的回调
            uint flag = 1;
            byte[] head = BitConverter.GetBytes(flag);
            head.CopyTo(sendBytes, 0);
            conv.CopyTo(sendBytes, 4);
            LocalSocket.SendTo(sendBytes, Remote);

            ReceiveHandle += OnReceive;
            DisconnectHandle += OnDisconnect;

        }

        private void OnTimeout() {
            Console.WriteLine("连接超时，请检查你的网络");
        }

        private void OnDisconnect(uint conv) {
            Console.WriteLine("客户端 " + conv + "断开");
        }

        private void OnAccept(byte[] arg1, int arg2) {
            Console.WriteLine("客户端收到接受了连接请求" + conv);
            ReceiveHandle += OnReceive;
        }

        private void OnReceive(uint conv, byte[] bytes, int length) {
            LastPingTime = GetTimeStamp();
            TimeoutTime = 0;
            if (length == 4) {
                uint msg = System.BitConverter.ToUInt32(bytes, 0);
                if (msg == 2) {
                    Pong();
                }
            }
        }

        public void Ping() {
            byte[] sendBytes = new byte[4];
            //2代表服务端发送的Ping
            uint flag = 2;
            byte[] head = System.BitConverter.GetBytes(flag);
            head.CopyTo(sendBytes, 0);
            Send(sendBytes);
        }

        public void Pong() {
            byte[] sendBytes = new byte[4];
            //3代表客户端发送的Pong
            uint flag = 3;
            byte[] head = System.BitConverter.GetBytes(flag);
            head.CopyTo(sendBytes, 0);
            Send(sendBytes);
        }



        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes">发送的数据</param>
        public void Send(Span<byte> bytes) {
            kcp.Send(bytes);
            Console.WriteLine("发送数据 " + " TO " + Remote + " " + conv);
        }

        /// <summary>
        /// Peer 的更新操作，负责接收来自udp的数据
        /// </summary>
        public void PeerUpdata() {

            kcp.Update(DateTime.UtcNow);
            var (temp, avalidSzie) = kcp.TryRecv();
            if (avalidSzie > 0) {
                byte[] receiveBytes = new byte[1024];
                temp.Memory.Span.Slice(0, avalidSzie).CopyTo(receiveBytes);
                if (ReceiveHandle != null) {
                    ReceiveHandle(conv, receiveBytes, avalidSzie);
                }

            }

        }


    }

    /// <summary>
    /// output 的回调，负责处理这个Peer的发送数据
    /// </summary>
    class Handle : IKcpCallback
    {
        public Socket socket;
        public EndPoint remote;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socket">本地的socket</param>
        /// <param name="remote">远端的IPEndPoint</param>
        public Handle(Socket socket, EndPoint remote) {
            this.socket = socket;
            this.remote = remote;

        }
        /// <summary>
        /// output回调
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="avalidLength"></param>
        public void Output(IMemoryOwner<byte> buffer, int avalidLength) {
            Span<byte> bytes = buffer.Memory.Slice(0, avalidLength).Span;
            if (socket != null && remote != null && avalidLength > 0) {
                socket.SendTo(bytes.ToArray(), remote);

            }
        }
    }
}
