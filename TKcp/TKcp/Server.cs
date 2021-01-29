using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Net.Sockets.TKcp
{
    public class Server
    {
        //以下应在peer里实现，各自管各自的心跳和收发消息,回调用action实现，心跳在peer里实现，在client里调用
        //disconnect
        //由客户端ping，客户端收到消息或者收到pong更新时间，一段时没收到就ping，长时间没收到就断开，
        //服务端在updatapeer时也检查ping，收到ping更新时间，长时间收不到断开连接,执行disconnecthandle

        Dictionary<uint, Peer> peerPool = new Dictionary<uint, Peer>();

        /// <summary>
        /// 服务端udp
        /// </summary>
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);



        public Server() {
            InitServer();
        }

        void InitServer() {
            socket.Bind(localIpep);
            Thread updataThread = new Thread(Updata);
            updataThread.Start();
            Thread updataPeerThread = new Thread(UpdataPeer);
            updataPeerThread.Start();

        }
        /// <summary>
        /// 连接号生成器
        /// </summary>
        /// <returns></returns>
        uint GenerateConv() {
            Random random = new Random();
            uint conv = ((uint)(random.Next(int.MinValue + 1000, int.MaxValue) - int.MinValue));
            while (peerPool.ContainsKey(conv)) {
                conv = ((uint)(random.Next(int.MinValue + 1000, int.MaxValue) - int.MinValue));
            }
            return conv;

        }
        /// <summary>
        /// 更新接收信息
        /// </summary>
        void Updata() {

            while (true) {
                if (socket.Available > 0) {
                    byte[] recvBuffer = new byte[socket.ReceiveBufferSize];
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    socket.ReceiveFrom(recvBuffer, ref remote);
                    //解析前四个byte的数据
                    byte[] convBytes = new byte[4];
                    Array.Copy(recvBuffer, 0, convBytes, 0, 4);
                    uint head = System.BitConverter.ToUInt32(convBytes);
                    //如果是连接请求
                    if (head == 0) {
                        //生成一个conv
                        uint conv = GenerateConv();
                        //创建一个peer，并初始化他
                        Peer peer = new Peer(socket, conv, remote);
                        peerPool.Add(conv, peer);
                        peer.ConnectHandle(System.BitConverter.GetBytes(conv), 4);

                        Console.WriteLine("接受了一个连接请求" + conv);

                    }
                    //如果是收到的消息
                    else {
                        peerPool[head].kcp.Input(recvBuffer);
                    }
                    recvBuffer = null;

                }
            }
        }


        /// <summary>
        /// 更新Peer
        /// </summary>
        void UpdataPeer() {
            
            while (true) {
                if (peerPool.Count <= 0) {
                    continue;
                }
                foreach (Peer peer in peerPool.Values) {
                    peer.PeerUpdata();
                }
            }
        }

    }
}
