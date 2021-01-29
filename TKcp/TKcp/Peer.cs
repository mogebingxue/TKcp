using System.Buffers;
using System.Net.Sockets.Kcp;

namespace System.Net.Sockets.TKcp
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

        Handle handle;
        public Kcp.Kcp kcp;

        public Model model;

        public enum Model
        {
            FAST = 0,
            NORMAL = 1
        }

        /// <summary>
        /// 应用层接收消息之后的回调，在应用层实现connection的时候要自己加上他的实现
        /// </summary>
        public Action<byte[], int> ReceiveHandle;
        /// <summary>
        /// 连接请求的回调，在这里要实现回传同意连接和连接号给客户端，应用层也可以有自己的附加实现
        /// </summary>
        public Action<byte[], int> ConnectHandle;
        /// <summary>
        /// 接收连接请求回调，客户端收到服务端的接收连接请求，之后的回调
        /// </summary>
        public Action<byte[], int> AcceptHandle;

        #region 构造函数
        public Peer(Socket socket, uint conv, EndPoint remote, Model model = 0) {
            this.LocalSocket = socket;
            this.conv = conv;
            this.Remote = remote;
            this.model = model;
            this.InitPeer();
        }
        #endregion

        /// <summary>
        /// 初始化Peer
        /// </summary>
        void InitPeer() {

            //初始化Kcp
            handle = new Handle(LocalSocket, Remote);
            kcp = new Kcp.Kcp(conv, handle);
            if (model == Model.FAST) {
                kcp.NoDelay(1, 10, 2, 1);//fast
            }
            else {
                kcp.NoDelay(0, 40, 0, 0);//normal
            }
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);
            ConnectHandle += OnConnect;
        }

        /// <summary>
        /// 给客户端发送连接号
        /// </summary>
        /// <param name="conv"></param>
        /// <param name="length"></param>
        private void OnConnect(byte[] conv, int length) {
            byte[] sendBytes = new byte[8];
            //1代表是同意连接的回调
            uint flag = 1;
            byte[] head = System.BitConverter.GetBytes(flag);
            head.CopyTo(sendBytes, 0);
            conv.CopyTo(sendBytes, 4);
            LocalSocket.SendTo(sendBytes, Remote);

            ReceiveHandle += OnReceive;
        }

        private void OnReceive(byte[] arg1, int arg2) {
            Console.WriteLine(System.Text.Encoding.UTF8.GetString(arg1));
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="bytes">发送的数据</param>
        public void Send(Span<byte> bytes) {
            kcp.Send(bytes);
            Console.WriteLine("应用层发送数据！");

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
                Console.WriteLine("应用层接收数据！");
                if (ReceiveHandle != null) {
                    ReceiveHandle(receiveBytes, avalidSzie);
                }

            }

        }


    }

    /// <summary>
    /// output 的回调，负责处理这个Peer的发送数据
    /// </summary>
    class Handle : IKcpCallback
    {
        Socket socket;
        EndPoint remote;
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
            if (socket != null) {
                socket.SendTo(bytes.ToArray(), remote);
            }
        }
    }
}
