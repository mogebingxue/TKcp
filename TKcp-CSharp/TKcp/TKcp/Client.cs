using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;

namespace TKcp
{
    public class Client
    {
        /// <summary>
        /// 重连时间
        /// </summary>
        public int Interval = 10;
        public Peer Peer;
        /// <summary>
        /// 客户端udp
        /// </summary>
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8890);
        long connectTime;
        IPEndPoint serverIpep;

        Thread updateAcceptThread;
        Thread updateThread;
        Thread updatePeerThread;

        public Client() {
            InitClient();
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public long GetTimeStamp() {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds);
        }

        /// <summary>
        /// 初始化客户端
        /// </summary>
        void InitClient() {
            Peer = new Peer(null, 0, null);
            socket.Bind(localIpep);
            updateAcceptThread = new Thread(UpdateAccept);
            updateThread = new Thread(Update);
            updatePeerThread = new Thread(UpdatePeer);
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="server">服务器的IPEndPoint</param>
        public void Connect(IPEndPoint server) {
            serverIpep = server;
            byte[] bytes = System.BitConverter.GetBytes(0);
            socket.SendTo(bytes, server);
            connectTime = GetTimeStamp();
            
            updateAcceptThread.Start();

        }
        /// <summary>
        /// 客户端发送数据
        /// </summary>
        /// <param name="sendbuffer">发送的数据</param>
        public void Send(byte[] sendbuffer) {
            if (Peer == null) {
                Console.WriteLine("未与服务器建立连接");
                return;
            }
            Peer.Send(sendbuffer);
            
        }

        /// <summary>
        /// 接收同意连接的消息
        /// </summary>
        void UpdateAccept() {
            while (true) {
                if (socket.Available > 0) {
                    byte[] recvBuffer = new byte[socket.ReceiveBufferSize];
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    socket.ReceiveFrom(recvBuffer, ref remote);
                    //解析前四个byte的数据
                    byte[] headBytes = new byte[4];
                    Array.Copy(recvBuffer, 0, headBytes, 0, 4);
                    uint head = System.BitConverter.ToUInt32(headBytes,0);

                    //如果是接受连接会送
                    if (head == 1) {
                        byte[] convBytes = new byte[4];
                        Array.Copy(recvBuffer, 4, convBytes, 0, 4);
                        uint conv = System.BitConverter.ToUInt32(convBytes,0);
                        Peer.LocalSocket = socket;
                        Peer.Conv = conv;
                        Peer.Remote = remote;
                        Peer.InitKcp();
                        if (Peer.AcceptHandle != null) {
                            Peer.AcceptHandle(System.BitConverter.GetBytes(conv), 4);
                        }
                        updateThread.Start();
                        updatePeerThread.Start();
                        
                        Thread.Sleep(Timeout.Infinite);
                        

                    }
                }
                else {
                    //现在的时间戳
                    long timeNow = GetTimeStamp();
                    //重连
                    if (timeNow - connectTime > Interval) {
                        byte[] bytes = System.BitConverter.GetBytes(0);
                        socket.SendTo(bytes, serverIpep);
                        connectTime = GetTimeStamp();
                        Peer.TimeoutTime++;
                    }
                    //超时
                    if (Peer.TimeoutTime >= 4) {
                        if (Peer.TimeoutHandle != null) {
                            Peer.TimeoutHandle();
                        }   
                    }
                }
            }
        }

        /// <summary>
        /// 更新接收信息
        /// </summary>
        void Update() {
            while (true) {

                if (socket.Available > 0) {
                    byte[] recvBuffer = new byte[socket.ReceiveBufferSize];
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    socket.ReceiveFrom(recvBuffer, ref remote);
                    //解析前四个byte的数据
                    byte[] headBytes = new byte[4];
                    Array.Copy(recvBuffer, 0, headBytes, 0, 4);
                    uint head = System.BitConverter.ToUInt32(headBytes,0);

                    //如果是收到的消息
                    if (head != 1) {
                        Peer.Kcp.Input(recvBuffer);
                    }
                }
            }
        }

        /// <summary>
        /// 更新Peer
        /// </summary>
        void UpdatePeer() {
            
            while (true) {
                if (Peer == null) {
                    continue;
                }
                Peer.PeerUpdate();
                
            }

        }
        #region 注册回调

        public void AddReceiveHandle(Action<uint,byte[],int> method) {
            Peer.ReceiveHandle += method;
        }

        public void AddAcceptHandle(Action<byte[], int> method) {
            Peer.AcceptHandle += method;
        }

        public void AddTimeoutHandle(Action method) {
            Peer.TimeoutHandle += method;
        }

        #endregion

    }


}
