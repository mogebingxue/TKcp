using System;
using System.Net;
using System.Net.Sockets.TKcp;
namespace TKcpDemo
{
    class Program
    {
        static void Main(string[] args) {

            IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
            TKcp tKcp1 = new TKcp(ipAdress,8888);
            TKcp tKcp2 = new TKcp(ipAdress, 8889);
            tKcp1.Send(System.Text.Encoding.Default.GetBytes("姚姚姚"),new IPEndPoint(ipAdress,8889));

            Span<byte> buffer = new byte[4096];
            while (true) {
                tKcp2.Receive(buffer);
                if (buffer[0]!=0) {
                    Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));
                    buffer[0] = 0;
                }
               
            }
          

        }
    }
}
