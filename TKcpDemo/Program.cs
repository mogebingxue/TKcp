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
        public static TKcp tKcp3;
        static void Main(string[] args) {

            IPAddress ipAdress = IPAddress.Parse("127.0.0.1");
            tKcp1 = new TKcp(ipAdress, 8888);
            tKcp2 = new TKcp(ipAdress, 8889);
            tKcp3 = new TKcp(ipAdress, 8890);
            //创建无参的线程
            Thread Loop = new Thread(new ThreadStart(StartLoop));
            //调用Start方法执行线程
            Loop.Start();
            tKcp1.Send(System.Text.Encoding.UTF8.GetBytes("姚姚姚雨廷"), new IPEndPoint(ipAdress, 8889));
            tKcp1.Send(System.Text.Encoding.UTF8.GetBytes("姚姚姚"), new IPEndPoint(ipAdress, 8890));
            //Thread.Sleep(1);
            //tKcp2.Send(System.Text.Encoding.Default.GetBytes("姚姚姚2"), new IPEndPoint(ipAdress, 8888));
            //tKcp1.Send(System.Text.Encoding.UTF8.GetBytes("姚姚姚3"), new IPEndPoint(ipAdress, 8890));


            static void StartLoop() {
                Span<byte> buffer = new byte[65536];
                while (true) {
                    tKcp2.Receive(buffer);
                    if (buffer[0] != 0) {
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));
                        //Console.WriteLine(tKcp2.remote);
                        //tKcp2.Send(System.Text.Encoding.Default.GetBytes("收到了"), tKcp2.remote);
                        buffer.Clear();
                    }
                    tKcp1.Receive(buffer);
                    if (buffer[0] != 0) {
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));
                        //Console.WriteLine(tKcp1.remote);

                        buffer.Clear();
                    }
                    tKcp3.Receive(buffer);
                    if (buffer[0] != 0) {
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(buffer));
                        //Console.WriteLine(tKcp3.remote);
                        //tKcp2.Send(System.Text.Encoding.Default.GetBytes("收到了"), tKcp2.remote);
                        buffer.Clear();
                    }
                }

            }
        }
    }
}
