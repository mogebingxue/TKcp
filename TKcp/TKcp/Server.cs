using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Net.Sockets.TKcp
{
    public class Server
    {
        /// <summary>
        /// 最大连接数
        /// </summary>
        public int MaxConnection = 1000;

        Peer[] peerpool;
        
        Dictionary<uint, Peer> peers = new Dictionary<uint, Peer>();

        Dictionary<uint, EndPoint> clients = new Dictionary<uint, EndPoint>();


        /// <summary>
        /// 服务端udp
        /// </summary>
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);
        private int pingInterval = 30;

        public Server() {
            InitServer();
        }

        void InitServer() {
            socket.Bind(localIpep);
            //初始化缓存池
            peerpool = new Peer[MaxConnection];
            for(int i = 0;i<peerpool.Length;i++) {
                peerpool[i] = new Peer(socket, (uint)(i + 1000), null);
            }
            
            
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
            uint conv = (uint)random.Next(1000, MaxConnection+1000);
            while (peers.ContainsKey(conv)) {
                conv = (uint)random.Next(1000, MaxConnection + 1000);
            }
            return conv;

        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public long GetTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        public void CheckPing(Peer peer) {
            //现在的时间戳
            long timeNow = GetTimeStamp();
            //Ping 一下
            if (timeNow - peer.LastPingTime > pingInterval) {
                Console.WriteLine("ping 一下");
                peer.Ping();
            }
            //遍历，删除
            if (timeNow - peer.LastPingTime > pingInterval * 4) {
                Console.WriteLine("超时删除");
                peer.DisconnectHandle();
                peers.Remove(peer.conv);
            }

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

                        if (clients.Count > MaxConnection) {
                            Console.WriteLine("已达到最大连接数");
                            continue;
                        }

                        //客户端已经连接，则不再连接
                        if (!clients.ContainsValue(remote)) {
                            //生成一个conv
                            uint conv = GenerateConv();
                            //从缓存池里一个peer，并初始化他
                            peerpool[conv-1000].Remote = remote;
                            peerpool[conv - 1000].InitKcp();
                            peers.Add(conv, peerpool[conv-1000]);
                            clients.Add(conv, remote);
                            peerpool[conv - 1000].ConnectHandle(System.BitConverter.GetBytes(conv));

                            Console.WriteLine("接受了一个连接请求" + remote + conv);
                        }
                        else {
                            Console.WriteLine("已经连接到服务器" + remote );
                        }

                    }
                    //如果是收到的消息
                    else {
                        peers[head].kcp.Input(recvBuffer);
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
                if (peers.Count <= 0) {
                    continue;
                }
                foreach (Peer peer in peers.Values) {
                    CheckPing(peer);
                    peer.PeerUpdata();
                }
            }
        }

        #region 注册回调

        public void AddReceiveHandle(Action<byte[], int> method) {
            foreach (Peer peer in peerpool) {
                peer.ReceiveHandle += method;
            }
        }

        public void AddConnectHandle(Action<byte[]> method) {
            foreach (Peer peer in peerpool) {
                peer.ConnectHandle += method;
            }
        }

        public void AddDisconnectHandle(Action method) {
            foreach (Peer peer in peerpool) {
                peer.DisconnectHandle += method;
            }
        }

        #endregion
    }
}
