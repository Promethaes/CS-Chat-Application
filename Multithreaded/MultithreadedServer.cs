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

                for (int i = 0; i < states.Count; i++)
                {
                    if (i == state.index)
                        continue;
                    handler.Send(state.buffer);
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
            IPAddress ipAddress = ipHostInfo.AddressList[4];
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

    class Client
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        EventWaitHandle connectWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle sendDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle receiveDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        public Client(IPEndPoint endPoint)
        {
            clientSocket.BeginConnect(endPoint, new AsyncCallback(ConnectionCallback), clientSocket);

            connectWaitHandle.WaitOne();
        }

        void ConnectionCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                client.EndAccept(ar);

                Console.Write("Socket Connected to {0}", client.RemoteEndPoint.ToString());

                connectWaitHandle.Set();
            }
            catch (Exception e)
            {
                Console.Write("Exception Caught: {0}", e);
            }
        }

        public void Send(string message)
        {
            byte[] sendBuffer = Encoding.ASCII.GetBytes(message);

            clientSocket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), clientSocket);
        }

        void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket)ar.AsyncState;

                int length = client.EndSend(ar);

                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void Receive()
        {
            try
            {
                StateObject state = new StateObject();
                state.workSocket = clientSocket;

                clientSocket.BeginReceive(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, new AsyncCallback(RecieveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        void RecieveCallback(IAsyncResult ar)
        {
            try
            {

                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                int length = client.EndReceive(ar);

                if (length > 0)
                {
                    state.finalString += Encoding.ASCII.GetString(state.buffer, 0, length);

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(RecieveCallback), state);
                }
                else
                {
                    receiveDone.Set();
                    Console.WriteLine(state.finalString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
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
