using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
namespace Multithreaded
{
    public class StateObject
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public string finalString = "";
        public int index = -1;
        public EndPoint remoteClient;
    }

    class Server
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        List<StateObject> states = new List<StateObject>();
        IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
        EventWaitHandle serverWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle readWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        void ReadCallback(IAsyncResult ar)
        {
            readWaitHandle.Set();
            try
            {

                StateObject state = (StateObject)ar.AsyncState;

                int length = serverSocket.EndReceiveFrom(ar, ref state.remoteClient);

                state.finalString = Encoding.ASCII.GetString(state.buffer, 0, length);
                Console.WriteLine(state.finalString);

                byte[] temp = new byte[256];
                temp = Encoding.ASCII.GetBytes("clin " + (states.Count - 1).ToString());
                serverSocket.SendTo(temp, state.remoteClient);
                serverSocket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, 0, ref state.remoteClient, new AsyncCallback(ReadCallback), state);

                byte[] buffer = new byte[1024];
                buffer = Encoding.ASCII.GetBytes(state.finalString);

                for (int i = 0; i < states.Count; i++)
                {
                    if (i == state.index)
                        continue;
                    serverSocket.SendTo(buffer, states[i].remoteClient);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }


        public Server()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());

            var hostAddr = IPAddress.Parse("192.168.0.1");
            foreach (var addr in ipHostInfo.AddressList)
            {
                if (addr.ToString().Contains("192"))
                    hostAddr = addr;
            }

            IPAddress ipAddress = hostAddr;
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 5000);

            serverSocket.Bind(endPoint);

        }

        public void StartServer()
        {
            while (true)
            {
                serverWaitHandle.Reset();

                var remoteClient = (EndPoint)client;
                byte[] buffer = new byte[1024];

                StateObject state = new StateObject();
                states.Add(state);
                state.index = states.Count - 1;
                state.remoteClient = remoteClient;

                try
                {
                    serverSocket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteClient, new AsyncCallback(ReadCallback), state);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                serverWaitHandle.WaitOne();
            }
        }
    }

    class MultithreadedServer
    {
        static void Main(string[] args)
        {
            Server server = new Server();
            Console.WriteLine("Server Started and Waiting For Connections...");
            server.StartServer();
        }
    }
}
