using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace System.Net.Sockets.TKcp
{
    public class Client
    {
        /// <summary>
        /// 客户端udp
        /// </summary>
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        IPEndPoint localIpep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8889);

        Peer peer;

        public Client() {
            InitClient();
        }


        /// <summary>
        /// 初始化客户端
        /// </summary>
        void InitClient() {
            socket.Bind(localIpep);
            Thread updataThread = new Thread(Updata);
            updataThread.Start();
            Thread updataPeerThread = new Thread(UpdataPeer);
            updataPeerThread.Start();
        }

        /// <summary>
        /// 连接服务器
        /// </summary>
        /// <param name="server">服务器的IPEndPoint</param>
        public void Connect(IPEndPoint server) {
            byte[] bytes = System.BitConverter.GetBytes(0);
            socket.SendTo(bytes, server);
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

                    //如果是接受连接会送
                    if (head == 1) {
                        byte[] convBytes = new byte[4];
                        Array.Copy(recvBuffer, 4, convBytes, 0, 4);
                        uint conv = System.BitConverter.ToUInt32(convBytes);
                        this.peer = new Peer(socket, conv, remote);
                        if (peer.AcceptHandle != null) {
                            peer.AcceptHandle(System.BitConverter.GetBytes(conv), 4);
                        }
                        

                        Console.WriteLine("客户端收到接受了连接请求" + conv);

                    }
                    //如果是收到的消息
                    else {
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
                if(peer == null) {
                    continue;
                }
                peer.PeerUpdata();
            }

        }

    }
}
