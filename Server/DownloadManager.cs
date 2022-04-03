﻿using System.IO;
using System.Linq;
using System.Collections.Generic;

using Lidgren.Network;
using System;

namespace CoopServer
{
    internal static class DownloadManager
    {
        private static readonly List<DownloadClient> _clients = new();
        private static readonly List<DownloadFile> _files = new();
        public static bool AnyFileExists = false;

        public static void InsertClient(long nethandle)
        {
            if (!AnyFileExists)
            {
                return;
            }

            _clients.Add(new DownloadClient(nethandle, _files));
        }

        public static bool CheckForDirectoryAndFiles()
        {
            string[] filePaths;

            if (!Directory.Exists("clientside"))
            {
                AnyFileExists = false;
                return false;
            }

            filePaths = Directory.GetFiles("clientside");
            if (filePaths.Length == 0)
            {
                AnyFileExists = false;
                return false;
            }

            byte fileCount = 0;

            foreach (string file in filePaths)
            {
                FileInfo fileInfo = new(file);

                // ONLY JAVASCRIPT AND JSON FILES!
                if (!new string[] { ".js", ".json" }.Any(x => x == fileInfo.Extension))
                {
                    Logging.Warning("Only files with \"*.js\" and \"*.json\" can be sent!");
                    continue;
                }

                int MAX_BUFFER = fileInfo.Length < 5120 ? (int)fileInfo.Length : 5120; // 5KB
                byte[] buffer = new byte[MAX_BUFFER];
                bool fileCreated = false;
                DownloadFile newFile = null;

                using (FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read))
                using (BufferedStream bs = new(fs))
                {
                    while (bs.Read(buffer, 0, MAX_BUFFER) != 0) // Reading 5KB chunks at time
                    {
                        if (!fileCreated)
                        {
                            newFile = new() { FileID = fileCount, FileName = fileInfo.Name, FileLength = fileInfo.Length };
                            fileCreated = true;
                        }

                        newFile.AddData(buffer);
                    }
                }

                _files.Add(newFile);
                fileCount++;
            }

            Logging.Info($"{_files.Count} files found!");

            AnyFileExists = true;
            return true;
        }

        public static void Tick()
        {
            _clients.ForEach(client =>
            {
                if (!client.SendFiles())
                {
                    Client x = Server.Clients.FirstOrDefault(x => x.NetHandle == client.NetHandle);
                    if (x != null)
                    {
                        x.FilesReceived = true;
                    }
                }
            });
        }

        public static void RemoveClient(long nethandle)
        {
            DownloadClient client = _clients.FirstOrDefault(x => x.NetHandle == nethandle);
            if (client != null)
            {
                _clients.Remove(client);
            }
        }

        /// <summary>
        /// We try to remove the client when all files have been sent
        /// </summary>
        /// <param name="nethandle"></param>
        /// <param name="id"></param>
        public static void TryToRemoveClient(long nethandle, int id)
        {
            DownloadClient client = _clients.FirstOrDefault(x => x.NetHandle == nethandle);
            if (client == null)
            {
                return;
            }

            client.FilePosition++;

            if (client.DownloadComplete())
            {
                _clients.Remove(client);
            }
        }
    }

    internal class DownloadClient
    {
        public long NetHandle = 0;
        private List<DownloadFile> Files = null;
        public int FilePosition = 0;

        public DownloadClient(long nethandle, List<DownloadFile> files)
        {
            NetHandle = nethandle;
            Files = files;

            NetConnection conn = Server.MainNetServer.Connections.FirstOrDefault(x => x.RemoteUniqueIdentifier == NetHandle);
            if (conn != null)
            {
                Files.ForEach(file =>
                {
                    NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();

                    new Packets.FileRequest()
                    {
                        ID = file.FileID,
                        FileType = (byte)Packets.DataFileType.Script,
                        FileName = file.FileName,
                        FileLength = file.FileLength
                    }.PacketToNetOutGoingMessage(outgoingMessage);

                    Server.MainNetServer.SendMessage(outgoingMessage, conn, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.File);
                });
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if files should be sent otherwise false</returns>
        public bool SendFiles()
        {
            if (DownloadComplete())
            {
                return false;
            }

            DownloadFile file = Files[FilePosition];

            file.Send(NetHandle);

            if (file.DownloadFinished())
            {
                FilePosition++;

                return DownloadComplete();
            }

            return true;
        }

        public bool DownloadComplete()
        {
            return FilePosition >= Files.Count;
        }
    }

    internal class DownloadFile
    {
        public byte FileID { get; set; }
        public string FileName { get; set; }
        public long FileLength { get; set; }

        private readonly List<byte[]> _data = new();
        private int _dataPosition = 0;

        public void Send(long nethandle)
        {
            NetConnection conn = Server.MainNetServer.Connections.FirstOrDefault(x => x.RemoteUniqueIdentifier == nethandle);
            if (conn == null)
            {
                return;
            }

            NetOutgoingMessage outgoingMessage = Server.MainNetServer.CreateMessage();

            new Packets.FileTransferTick() { ID = FileID, FileChunk = _data.ElementAt(_dataPosition) }.PacketToNetOutGoingMessage(outgoingMessage);

            Server.MainNetServer.SendMessage(outgoingMessage, conn, NetDeliveryMethod.ReliableOrdered, (byte)ConnectionChannel.File);

            Logging.Debug($"Send _data[{_dataPosition}] ~ {_data.ElementAt(_dataPosition).Length}");

            _dataPosition++;
        }

        public void AddData(byte[] data)
        {
            _data.Add(data);
        }

        public void Cancel()
        {
            _dataPosition = _data.Count - 1;
        }

        public bool DownloadFinished()
        {
            return _dataPosition >= _data.Count;
        }
    }
}