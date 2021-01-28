using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
//https://docs.microsoft.com/en-us/dotnet/framework/network-programming/listening-with-sockets
namespace SyncronousChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //create TCP/IP server socket
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ipAddress = IPAddress.Loopback;
            IPEndPoint endpoint = new IPEndPoint(ipAddress, 12345);

            //bind the socket to the port to listen
            serverSocket.Bind(endpoint);

            //allow the socket to accept up to 100 connections
            serverSocket.Listen(100);
            Console.WriteLine("Waiting for a connection...");

            //wait until a connection is recieved. This function is blocking.
            Socket connection = serverSocket.Accept();
            Console.WriteLine("Recieved connection from " + connection.LocalEndPoint.ToString());

            while (true)
            {
                //create message buffer to hold incoming message
                byte[] recBuffer = new byte[1024];

                //receive the incoming string, store length and bytes in respective variables
                try
                {
                    int length = connection.Receive(recBuffer);
                    //store and print the message, decode the bytes to ASCII
                    string message = Encoding.ASCII.GetString(recBuffer, 0, length);
                    Console.WriteLine(message);
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
            //Shutdown and close sockets/connection
            connection.Shutdown(SocketShutdown.Both);
            connection.Close();

        }
    }
}
