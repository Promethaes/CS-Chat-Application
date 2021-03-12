﻿using System;
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
        public int index = -1;
        public int sessionId = -1;
    }

    public class Room
    {
        public Dictionary<string, StateObject> currentRoomOccupents = new Dictionary<string, StateObject>();
        public int sessionID = -1;
    }

    class Server
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        Dictionary<string, StateObject> states = new Dictionary<string, StateObject>();

        EventWaitHandle serverWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        List<Room> rooms = new List<Room>();


        public int maxRooms = 5;
        public int occupiedRooms = -1;

        bool _RegisterClient(ref StateObject state)
        {
            foreach (var room in rooms)
                if (room.currentRoomOccupents.ContainsKey(state.remoteClient.ToString()))
                    return false;

            var parts = state.finalString.Split(' ');
            state.sessionId = int.Parse(parts[1]);

            //create a new room
            if (state.sessionId == -1 /*&& rooms.Count < maxRooms*/)
            {
                rooms.Add(new Room());
                state.sessionId = rooms.Count - 1;
            }
            else if (state.sessionId > rooms.Count - 1)
                return false;

            int sID = rooms.Count - 1;
            rooms[sID].currentRoomOccupents.Add(state.remoteClient.ToString(), state);
            state.index = rooms[sID].currentRoomOccupents.Count - 1;
            var tempMsg = Encoding.ASCII.GetBytes("clin " + state.index.ToString() + " " + state.sessionId.ToString());
            serverSocket.SendTo(tempMsg, state.remoteClient);

            var toOthers = Encoding.ASCII.GetBytes("spawn " + state.index.ToString());

            foreach (var client in rooms[sID].currentRoomOccupents)
            {
                if (client.Key == state.remoteClient.ToString())
                    continue;

                var toSelf = Encoding.ASCII.GetBytes("spawn " + client.Value.index.ToString());

                serverSocket.SendTo(toOthers, client.Value.remoteClient);
                serverSocket.SendTo(toSelf, state.remoteClient);
            }

            return true;
        }

        void _Disconnect(ref StateObject state)
        {
            var temp = Encoding.ASCII.GetBytes("Goodbye");
            serverSocket.SendTo(temp, state.remoteClient);

            StateObject obj = null;
            foreach (var room in rooms)
            {
                if (room.currentRoomOccupents.ContainsKey(state.remoteClient.ToString()))
                {
                    obj = room.currentRoomOccupents[state.remoteClient.ToString()];
                    break;
                }
            }

            foreach (var client in rooms[obj.sessionId].currentRoomOccupents)
            {
                if (client.Key == obj.remoteClient.ToString())
                    continue;
                temp = Encoding.ASCII.GetBytes("remove " + obj.index);
                serverSocket.SendTo(temp, client.Value.remoteClient);
            }

            //remove client from room
            rooms[obj.sessionId].currentRoomOccupents.Remove(obj.remoteClient.ToString());

            Console.WriteLine(state.remoteClient.ToString() + " Disconnected");

            serverWaitHandle.Set();
        }

        void _RelayMessage(ref StateObject state, int length)
        {
            foreach (var client in rooms[state.sessionId].currentRoomOccupents)
            {
                if (client.Key == state.remoteClient.ToString())
                    continue;
                serverSocket.SendTo(state.buffer, length, SocketFlags.None, client.Value.remoteClient);
            }
        }

        void ReadCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;

                int length = serverSocket.EndReceiveFrom(ar, ref state.remoteClient);

                state.finalString = Encoding.ASCII.GetString(state.buffer, 0, length);

                if (!_RegisterClient(ref state))
                {
                    foreach (var room in rooms)
                        if (room.currentRoomOccupents.ContainsKey(state.remoteClient.ToString()))
                            state.sessionId = room.currentRoomOccupents[state.remoteClient.ToString()].sessionId;
                }

                if (state.finalString == "endMsg")
                {
                    _Disconnect(ref state);
                    return;
                }

                Console.WriteLine(state.finalString);

                _RelayMessage(ref state, length);

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
