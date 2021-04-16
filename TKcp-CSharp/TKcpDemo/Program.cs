using System;
using System.Net;
using System.Net.Sockets;
using TKcp;
using System.Threading;

namespace TKcpDemo
{
    class Program
    {
        static void Main(string[] args) {

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8887);

            Client client = new Client();
            client.AddReceiveHandle(test);
            Server server = new Server();
            server.AddReceiveHandle(test);
            client.Connect(iPEndPoint);
            Console.ReadKey();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("姚姚姚");
            client.Send(bytes);
            server.Send(client.Peer.Conv, bytes);
            void test(uint conv, byte[] bytes, int length) {
                Console.WriteLine("注册了一个回调");
                Console.WriteLine(System.Text.Encoding.UTF8.GetString(bytes));
            }
            while (true) {

            }

            


        }
        
    }
}
