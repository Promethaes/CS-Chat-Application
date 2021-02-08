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
        public EndPoint remoteClient;
    }

    class Server
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        Dictionary<string,StateObject> states = new Dictionary<string, StateObject>();
        
        EventWaitHandle serverWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        void ReadCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                int length = serverSocket.EndReceiveFrom(ar, ref state.remoteClient);

                if (!states.ContainsKey(state.remoteClient.ToString()))
                {
                    states.Add(state.remoteClient.ToString(), state);

                    var temp = Encoding.ASCII.GetBytes("clin " + (states.Count - 1).ToString() + " ");
                    serverSocket.SendTo(temp, state.remoteClient);
                }
                
                state.finalString = Encoding.ASCII.GetString(state.buffer, 0, length);
                if(state.finalString == "endMsg")
                {
                    var temp = Encoding.ASCII.GetBytes("Goodbye");
                    serverSocket.SendTo(temp, state.remoteClient);

                    states.Remove(state.remoteClient.ToString());

                    Console.WriteLine(state.remoteClient.ToString() + " Disconnected");

                    serverWaitHandle.Set();
                    return;
                }

                Console.WriteLine(state.finalString);
               
                foreach(var ep in states)
                {
                    if (ep.Key == state.remoteClient.ToString())
                        continue;

                    serverSocket.SendTo(state.buffer,length,SocketFlags.None, ep.Value.remoteClient);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            serverWaitHandle.Set();

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

                IPEndPoint client = new IPEndPoint(IPAddress.Any, 0);
                var remoteClient = (EndPoint)client;
                byte[] buffer = new byte[1024];

                StateObject state = new StateObject();
                state.remoteClient = remoteClient;

                try
                {
                    serverSocket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, ref state.remoteClient, new AsyncCallback(ReadCallback), state);
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
