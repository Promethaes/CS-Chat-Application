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
        public Socket workSocket = null;
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public string finalString = "";
        public int index = -1;
    }

    class Server
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        List<StateObject> states = new List<StateObject>();

        void AcceptCallback(IAsyncResult ar)
        {
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            acceptWaitHandle.Set();

            Console.WriteLine("Connection Established with {0}", handler.RemoteEndPoint.ToString());


            StateObject state = new StateObject();
            states.Add(state);
            state.index = states.Count - 1;
            state.workSocket = handler;

            handler.Send(Encoding.ASCII.GetBytes("cli" + " " + state.index.ToString()));

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        void ReadCallback(IAsyncResult ar)
        {
            try
            {

                StateObject state = (StateObject)ar.AsyncState;
                Socket handler = state.workSocket;

                int length = handler.EndReceive(ar);

                state.finalString = Encoding.ASCII.GetString(state.buffer, 0, length);
                Console.WriteLine(state.finalString);

                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);

                byte[] buffer = new byte[1024];
                buffer = Encoding.ASCII.GetBytes(state.finalString);

                for (int i = 0; i < states.Count; i++)
                {
                    if (i == state.index)
                        continue;
                    states[i].workSocket.Send(buffer);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        void SendCallback(IAsyncResult ar)
        {
            Socket handler = (Socket)ar.AsyncState;
            handler.EndSend(ar);
        }

        EventWaitHandle acceptWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);


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
            serverSocket.Listen(100);


        }

        public void StartServer()
        {
            while (true)
            {
                acceptWaitHandle.Reset();
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), serverSocket);
                acceptWaitHandle.WaitOne();
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
