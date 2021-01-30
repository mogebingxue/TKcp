using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace System.Net.Sockets.TKcp
{
    public class Client
    {
        /// <summary>
        /// 重连时间
        /// </summary>
        public int Interval = 10;
        /// <summary>
        /// 客户端udp
        /// </summary>
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        
        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8889);

        Peer peer;

        long connectTime;

        IPEndPoint serverIpep;

        Thread updataAcceptThread;
        Thread updataThread;
        Thread updataPeerThread;

        public Client() {
            peer = new Peer(null, 0, null);
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
            socket.Bind(localIpep);
            updataAcceptThread = new Thread(UpdataAccept);
            updataThread = new Thread(Updata);
            updataPeerThread = new Thread(UpdataPeer);
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
            
            updataAcceptThread.Start();

        }
        /// <summary>
        /// 客户端发送数据
        /// </summary>
        /// <param name="sendbuffer">发送的数据</param>
        public void Send(byte[] sendbuffer) {
            if (peer == null) {
                Console.WriteLine("未与服务器建立连接");
                return;
            }
            peer.Send(sendbuffer);
            
        }

        /// <summary>
        /// 接收同意连接的消息
        /// </summary>
        void UpdataAccept() {
            //重连次数
            int time = 0;
            while (true) {
                if (socket.Available > 0) {
                    byte[] recvBuffer = new byte[socket.ReceiveBufferSize];
                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    socket.ReceiveFrom(recvBuffer, ref remote);
                    //解析前四个byte的数据
                    byte[] headBytes = new byte[4];
                    Array.Copy(recvBuffer, 0, headBytes, 0, 4);
                    uint head = System.BitConverter.ToUInt32(headBytes);

                    //如果是接受连接会送
                    if (head == 1) {
                        byte[] convBytes = new byte[4];
                        Array.Copy(recvBuffer, 4, convBytes, 0, 4);
                        uint conv = System.BitConverter.ToUInt32(convBytes);
                        peer.LocalSocket = socket;
                        peer.conv = conv;
                        peer.Remote = remote;
                        peer.InitKcp();
                        if (peer.AcceptHandle != null) {
                            peer.AcceptHandle(System.BitConverter.GetBytes(conv), 4);
                        }
                        updataThread.Start();
                        updataPeerThread.Start();
                        
                        
                        Thread.Sleep(Timeout.Infinite);
                        

                    }
                }
                else {
                    //现在的时间戳
                    long timeNow = GetTimeStamp();
                    //重连
                    if (timeNow - connectTime > 10) {
                        byte[] bytes = System.BitConverter.GetBytes(0);
                        socket.SendTo(bytes, serverIpep);
                        connectTime = GetTimeStamp();
                        time++;
                    }
                    //超时
                    if (time>=4) {
                        if (peer.TimeoutHandle != null) {
                            peer.TimeoutHandle();
                        }   
                    }
                }
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
                    byte[] headBytes = new byte[4];
                    Array.Copy(recvBuffer, 0, headBytes, 0, 4);
                    uint head = System.BitConverter.ToUInt32(headBytes);

                    //如果是收到的消息
                    if (head != 1) {
                        peer.kcp.Input(recvBuffer);
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
                if (peer == null) {
                    continue;
                }
                peer.PeerUpdata();
                
            }

        }
        #region 注册回调

        public void AddReceiveHandle(Action<byte[],int> method) {
            peer.ReceiveHandle += method;
        }

        public void AddAcceptHandle(Action<byte[], int> method) {
            peer.AcceptHandle += method;
        }

        public void AddTimeoutHandle(Action method) {
            peer.TimeoutHandle += method;
        }

        #endregion

    }


}
