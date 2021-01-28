using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
//https://docs.microsoft.com/en-us/dotnet/framework/network-programming/using-client-sockets
namespace SyncronousChat
{
    class Program
    {
        static void Main(string[] args)
        {

            //creating the client socket, that uses TCP/IP
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //loopback address to test this on my own computer
            IPAddress address = IPAddress.Loopback;
            IPEndPoint endPoint = new IPEndPoint(address, 12345);

            //setting up connection
            clientSocket.Connect(endPoint);


            while (true)
            {
                string message = address.ToString() + ": " + Console.ReadLine();

                byte[] sendBuf = Encoding.ASCII.GetBytes(message);
                try
                {
                    clientSocket.Send(sendBuf);

                    byte[] recBuffer = new byte[1024];
                    int length = clientSocket.Receive(recBuffer);
                    string message2 = Encoding.ASCII.GetString(recBuffer, 0, length);
                    Console.WriteLine(message2);

                }
                catch (SocketException e)
                {
                    Console.WriteLine("Socket Exception {0}", e);
                    break;
                }
                catch (ObjectDisposedException e)
                {
                    Console.WriteLine("ObjectDisposedException {0}", e);
                    break;
                }
                catch (ArgumentException e)
                {
                    Console.WriteLine("ArgumentException {0}", e);
                    break;
                }

            }
        }
    }
}
