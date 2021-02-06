using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
namespace MultithreadedClient
{
    public class StateObject
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public string finalString = "";
        public int index = -1;
        public EndPoint remoteClient;
    }
    class Client
    {
        public Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        public Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        EventWaitHandle sendDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle receiveDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        public IPEndPoint endPoint;
        public Client()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.0.46");
            endPoint = new IPEndPoint(ipAddress, 5000);

            clientSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
           
        }

        public void Send(string message)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(message);

            clientSocket.BeginSendTo(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, endPoint, new AsyncCallback(SendCallback), clientSocket);
        }

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int length = client.EndSendTo(ar);

                sendDone.Set();

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Receive()
        {
            while (true)
            {
                receiveDone.Reset();
                try
                {
                    byte[] buffer = new byte[1024];
                    var ep = (EndPoint)endPoint;
                    clientSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref ep);

                    var fString = Encoding.ASCII.GetString(buffer);
                    
                    Console.WriteLine(fString);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                receiveDone.Set();
            }
        }
    }

    class MultithreadedClient
    {
        static void Main(string[] args)
        {
            EventWaitHandle mainWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
            Client client = new Client();
            Thread recThread = new Thread(client.Receive);
            recThread.Start();

            //byte[] buffer = Encoding.ASCII.GetBytes("initMsg");
            //client.clientSocket.SendTo(buffer, client.endPoint);
            while (true)
            {
                mainWaitHandle.Reset();

                client.Send(Console.ReadLine());
            }
        }
    }
}
