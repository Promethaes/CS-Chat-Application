﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO;

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
        public string username = "";
        public bool markedForDelete = false;
    }

    public class Room
    {
        public Dictionary<string, StateObject> currentRoomOccupents = new Dictionary<string, StateObject>();
        public int sessionID = -1;
        public string roomName = "";
    }

    public class Leaderboard
    {
        //should have the entry name as the key
        public Dictionary<string, LeaderboardEntry> entries = new Dictionary<string, LeaderboardEntry>();

        //param: true: clears the dictionary of entries upon completion
        public void WriteToFile(bool clearEntriesUponCompletion)
        {
            while (entries.Count > 10)
            {
                foreach (var entry in entries)
                {
                    entries.Remove(entry.Key);
                    break;
                }
            }

            string fstring = "";
            foreach (var entry in entries)
            {
                fstring += "Entry " + entry.Value.name + " \n";
                fstring += "EnemiesDef " + entry.Value.numEnemiesDefeated + " \n";
                fstring += "Time " + entry.Value.timeTaken + " \n";
            }

            File.WriteAllText("leaderboard.txt", fstring);


            if (clearEntriesUponCompletion)
                entries.Clear();
        }

        //Reads leaderboard data in from file
        public Dictionary<string, LeaderboardEntry> ReadFromFile(bool storeIntoClass)
        {

            if (!File.Exists(@".\leaderboard.txt"))
            {
                Console.WriteLine("Tried to open ./leaderboard.txt, but it doesn't exist!");
                return entries;
            }

            StreamReader reader = File.OpenText(@".\leaderboard.txt");

            string content = null;
            Dictionary<string, LeaderboardEntry> localEntries = new Dictionary<string, LeaderboardEntry>();
            while ((content = reader.ReadLine()) != null)
            {
                LeaderboardEntry entry = new LeaderboardEntry();
                //name
                {
                    var parts = content.Split(' ');
                    entry.name = parts[1];
                }

                //enemies defeated
                content = reader.ReadLine();
                {
                    var parts = content.Split(' ');
                    entry.numEnemiesDefeated = int.Parse(parts[1]);
                }

                //time taken
                content = reader.ReadLine();
                {
                    var parts = content.Split(' ');
                    entry.timeTaken = parts[1];
                }
                localEntries.Add(entry.name, entry);
            }
            reader.Close();

            if (!storeIntoClass)
                return localEntries;

            entries = localEntries;
            return entries;
        }

        public override string ToString()
        {
            string fstring = null;

            foreach (var entry in entries)
                fstring += "?" + entry.Value.ToString();

            return fstring;
        }
    }
    public class LeaderboardEntry
    {
        public string name;
        public int numEnemiesDefeated;
        public string timeTaken;

        public override string ToString()
        {
            string fstring = "";

            fstring += name + " ";
            fstring += numEnemiesDefeated.ToString() + " ";
            fstring += timeTaken;

            return fstring;
        }
    }

    class Server
    {
        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        EventWaitHandle serverWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);

        List<Room> rooms = new List<Room>();

        Leaderboard leaderboard = new Leaderboard();


        public int maxRooms = 5;
        public int occupiedRooms = -1;

        void _SendRoomInfo(ref StateObject state)
        {
            foreach (var room in rooms)
            {
                //room SID numberOfOccupents
                string finalMessage = "";
                finalMessage = "room " + room.sessionID + " " + room.currentRoomOccupents.Count + " " + room.roomName;
                Console.WriteLine(finalMessage);

                serverSocket.SendTo(Encoding.ASCII.GetBytes(finalMessage), state.remoteClient);
            }

            if (rooms.Count == 0)
                serverSocket.SendTo(Encoding.ASCII.GetBytes("no"), state.remoteClient);

            state.markedForDelete = true;
        }



        bool _RegisterClient(ref StateObject state)
        {
            foreach (var room in rooms)
                if (room.currentRoomOccupents.ContainsKey(state.remoteClient.ToString()))
                    return false;

            var parts = state.finalString.Split(' ');
            state.sessionId = int.Parse(parts[1]);
            state.username = parts[2];
            //create a new room
            if (state.sessionId == -1)
            {
                rooms.Add(new Room());
                state.sessionId = rooms.Count - 1;
                rooms[rooms.Count - 1].sessionID = state.sessionId;
                rooms[rooms.Count - 1].roomName = state.username;
            }


            int sID = rooms.Count - 1;
            rooms[sID].currentRoomOccupents.Add(state.remoteClient.ToString(), state);
            state.index = rooms[sID].currentRoomOccupents.Count - 1;
            var tempMsg = Encoding.ASCII.GetBytes("clin " + state.index.ToString() + " " + state.sessionId.ToString());
            serverSocket.SendTo(tempMsg, state.remoteClient);

            var toOthers = Encoding.ASCII.GetBytes("spawn " + state.index.ToString() + " " + state.username);

            foreach (var client in rooms[sID].currentRoomOccupents)
            {
                if (client.Key == state.remoteClient.ToString())
                    continue;

                var toSelf = Encoding.ASCII.GetBytes("spawn " + client.Value.index.ToString() + " " + client.Value.username);

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

                if (state.finalString == "roominfo")
                {
                    Console.WriteLine(state.remoteClient.ToString() + " is requesting room info");
                    _SendRoomInfo(ref state);
                    serverWaitHandle.Set();
                    return;
                }
                else if (state.finalString == "leaderinfo")
                {
                    Console.WriteLine(state.remoteClient.ToString() + " is requesting leaderboard info");

                    var buffer = new byte[1024];
                    buffer = Encoding.ASCII.GetBytes(leaderboard.ToString());
                    Console.Write(leaderboard.ToString());
                    serverSocket.SendTo(buffer, state.remoteClient);
                    serverWaitHandle.Set();
                    return;

                }
                else if (state.finalString.Contains("ldr"))
                {
                    var parts = state.finalString.Split(' ');
                    foreach (var room in rooms)
                    {
                        foreach (var occupent in room.currentRoomOccupents)
                        {
                            if (parts[0] == occupent.Value.username)
                            {
                                LeaderboardEntry entry = new LeaderboardEntry();
                                entry.name = occupent.Value.username;
                                entry.numEnemiesDefeated = int.Parse(parts[3]);
                                entry.timeTaken = parts[4];
                                if (leaderboard.entries.ContainsKey(entry.name))
                                    leaderboard.entries.Remove(entry.name);
                                leaderboard.entries[entry.name] = entry;
                                leaderboard.WriteToFile(clearEntriesUponCompletion: false);
                                Console.WriteLine("Wrote to file");
                                serverWaitHandle.Set();
                                return;
                            }
                        }
                    }
                }

                if (state.finalString == "endMsg")
                {
                    _Disconnect(ref state);
                    serverWaitHandle.Set();
                    return;
                }

                if (!_RegisterClient(ref state))
                {
                    foreach (var room in rooms)
                        if (room.currentRoomOccupents.ContainsKey(state.remoteClient.ToString()))
                            state.sessionId = room.currentRoomOccupents[state.remoteClient.ToString()].sessionId;
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

            //var hostAddr = IPAddress.Parse("172.31.81.144");
            var hostAddr = IPAddress.Parse("192.168.0.46");
            //foreach (var addr in ipHostInfo.AddressList)
            //{
            //    if (addr.ToString().Contains("192"))
            //        hostAddr = addr;
            //}

            IPAddress ipAddress = hostAddr;
            IPEndPoint endPoint = new IPEndPoint(ipAddress, 5000);

            serverSocket.Bind(endPoint);
            leaderboard.ReadFromFile(storeIntoClass: true);

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
                    serverWaitHandle.Set();

                }

                state = null;
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].currentRoomOccupents.Count == 0)
                    {
                        rooms.RemoveAt(i);
                        i--;
                    }
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
