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
        EventWaitHandle connectWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle sendDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        EventWaitHandle receiveDone = new EventWaitHandle(true, EventResetMode.ManualReset);
        IPEndPoint endPoint;
        public Client()
        {
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = IPAddress.Parse("192.168.0.46");
            endPoint = new IPEndPoint(ipAddress, 5000);


            connectWaitHandle.WaitOne();
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
            try
            {
                StateObject state = new StateObject();
                state.remoteClient = (EndPoint)endPoint;
                EndPoint temp = new IPEndPoint(IPAddress.Any, 0);
                clientSocket.Bind(temp);
                clientSocket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, ref state.remoteClient, new AsyncCallback(RecieveCallback), state);
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

                int length = clientSocket.EndReceiveFrom(ar, ref state.remoteClient);

                state.finalString = Encoding.ASCII.GetString(state.buffer, 0, length);
                Console.WriteLine(state.finalString);

                clientSocket.BeginReceiveFrom(state.buffer, 0, StateObject.BufferSize, SocketFlags.None, ref state.remoteClient, new AsyncCallback(RecieveCallback), state);
                receiveDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }


    class MultithreadedClient
    {
        static void Main(string[] args)
        {
            Client client = new Client();
            Thread recThread = new Thread(client.Receive);
            if (recThread.ThreadState == ThreadState.Unstarted)
                recThread.Start();
            while (true)
            {
                client.Send(Console.ReadLine());
            }
        }
    }
}
