using System;
using System.Net;
using System.Net.Sockets;
using System.Net.Sockets.TKcp;
using System.Threading;

namespace TKcpDemo
{
    class Program
    {
        static void Main(string[] args) {

            IPEndPoint iPEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8888);

            Client client = new Client();
            Server server = new Server();
            client.Connect(iPEndPoint);
            Console.ReadKey();
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes("姚姚姚");
            client.Send(bytes);
            while (true) {

            }




        }
    }
}
