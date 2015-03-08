using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TNL.NET.Network
{
    public enum NetError
    {
        NoError,
        InvalidPacketProtocol,
        WouldBlock,
        UnknownError
    }

    public class TNLSocket
    {
        public const UInt32 MaxPacketDataSize = 1490;

        private Boolean _needRun;
        private readonly UdpClient _socket;

        public Queue<Tuple<IPEndPoint, Byte[]>> PacketsToBeHandled = new Queue<Tuple<IPEndPoint, Byte[]>>();

        public TNLSocket()
        {
            _socket = new UdpClient();
        }

        public TNLSocket(Int32 port)
        {
            _socket = new UdpClient(port);
            _socket.BeginReceive(OnEndReceive, null);

            _needRun = true;
        }

        private void OnEndReceive(IAsyncResult result)
        {
            try
            {
                var ep = new IPEndPoint(0, 0);

                var buff = _socket.EndReceive(result, ref ep);

                if (buff != null && buff.Length > 0)
                {
                    PacketsToBeHandled.Enqueue(new Tuple<IPEndPoint, Byte[]>(ep, buff));

                    using (var sw = new StreamWriter("received.txt", true, Encoding.UTF8))
                    {
                        sw.WriteLine(BitConverter.ToString(buff));
                        sw.WriteLine();
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Socket closed, stopping Receiving!");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.ConnectionReset)
                {
                    Console.WriteLine("Valami hiba (fogadás)!");
                    Console.WriteLine(se);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Valami hiba (fogadás)!");
                Console.WriteLine(e);
            }

            if (_needRun && _socket != null)
                _socket.BeginReceive(OnEndReceive, null);
        }

        public void Stop()
        {
            _needRun = false;
        }

        public NetError Send(IPEndPoint iep, Byte[] buffer, UInt32 bufferSize)
        {
            using (var sw = new StreamWriter("sent-processed.txt", true, Encoding.UTF8))
            {
                sw.WriteLine(BitConverter.ToString(buffer, 0, (Int32) bufferSize));
                sw.WriteLine();
            }

            try
            {
                _socket.BeginSend(buffer, (Int32) bufferSize, iep, OnEndSend, null);

                return NetError.NoError;
            }
            catch
            {
                return NetError.UnknownError;
            }
        }

        public void OnEndSend(IAsyncResult result)
        {
            try
            {
                _socket.EndSend(result);
            }
            catch
            {
                Console.WriteLine("Valami hiba (küldés)!");
            }

        }

        public void Connect(IPEndPoint ep)
        {
            _socket.Connect(ep);
            _socket.BeginReceive(OnEndReceive, null);

            _needRun = true;
        }
    }
}
