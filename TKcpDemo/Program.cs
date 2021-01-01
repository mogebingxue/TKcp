using System;
using System.Net;
using System.Net.Sockets.TKcp;
using System.Threading;

namespace TKcpDemo
{
    class Program
    {
        public static TKcp tKcp1;
        public static TKcp tKcp2;
        static void Main(string[] args) {

            IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
            tKcp1 = new TKcp(ipAdress, 8888);
            tKcp2 = new TKcp(ipAdress, 8889);
            //创建无参的线程
            Thread Loop = new Thread(new ThreadStart(StartLoop));
            //调用Start方法执行线程
            Loop.Start();
            tKcp1.Send(System.Text.Encoding.Default.GetBytes("姚姚姚1"), new IPEndPoint(ipAdress, 8889));
            //Thread.Sleep(500);
            for(long i = 0; i < 10000000; i++) {

            }
            tKcp2.Send(System.Text.Encoding.Default.GetBytes("姚姚姚2"), new IPEndPoint(ipAdress, 8888));
           
            
            static void StartLoop() {
                Span<byte> buffer = new byte[4096];
                while (true) {
                    tKcp2.Receive(buffer);
                    if (buffer[0] != 0) {
                        Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));
                        Console.WriteLine(tKcp2.remote);
                        tKcp2.Send(System.Text.Encoding.Default.GetBytes("收到了"), tKcp2.remote);
                        buffer[0] = 0;
                    }
                    tKcp1.Receive(buffer);
                    if (buffer[0] != 0) {
                        Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));
                        Console.WriteLine(tKcp1.remote);

                        buffer[0] = 0;
                    }
                }

            }


        }


    }

}
