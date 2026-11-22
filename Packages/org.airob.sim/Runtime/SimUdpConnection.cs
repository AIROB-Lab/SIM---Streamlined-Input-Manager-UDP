using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Sim
{
    /// <summary>UTC seconds anchored once to a monotonic clock; safe to read on callback threads.</summary>
    public static class SimClock
    {
        static readonly double origin = (DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
        static readonly Stopwatch watch = Stopwatch.StartNew();
        public static double Now => origin + watch.Elapsed.TotalSeconds;
    }

    /// <summary>UDP transport only. Drain received packets on the owning application thread.</summary>
    public sealed class SimUdpConnection : IDisposable
    {
        readonly UdpClient client;
        readonly ConcurrentQueue<SimPacket> queue = new ConcurrentQueue<SimPacket>();
        volatile bool disposed;
        int queued, invalid, dropped;
        public int InvalidPackets => Volatile.Read(ref invalid);
        public int DroppedPackets => Volatile.Read(ref dropped);
        public string LastError { get; private set; }
        public const int QueueLimit = 2048;

        /// <param name="receivePort">Null creates a send-only client; otherwise bind and receive.</param>
        public SimUdpConnection(int? receivePort = null)
        {
            if (receivePort.HasValue && (receivePort < 1 || receivePort > 65535)) throw new ArgumentOutOfRangeException(nameof(receivePort));
            client = receivePort.HasValue ? new UdpClient(receivePort.Value) : new UdpClient();
            if (receivePort.HasValue) client.BeginReceive(Receive, null);
        }

        void Receive(IAsyncResult result)
        {
            try
            {
                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = client.EndReceive(result, ref sender);
                if (disposed) return;
                if (SimPacketCodec.TryDecode(bytes, out var packet))
                {
                    packet.ReceivedAtSeconds = SimClock.Now;
                    if (Interlocked.Increment(ref queued) <= QueueLimit) queue.Enqueue(packet);
                    else { Interlocked.Decrement(ref queued); Interlocked.Increment(ref dropped); }
                }
                else Interlocked.Increment(ref invalid);
            }
            catch (ObjectDisposedException) { return; }
            catch (SocketException e) { if (!disposed) LastError = e.Message; }
            finally
            {
                if (!disposed)
                {
                    try { client.BeginReceive(Receive, null); }
                    catch (ObjectDisposedException) { }
                    catch (SocketException e) { LastError = e.Message; }
                }
            }
        }

        public bool TryReceive(out SimPacket packet)
        {
            if (!queue.TryDequeue(out packet)) return false;
            Interlocked.Decrement(ref queued);
            return true;
        }

        public void Send(byte[] bytes, string host, int port)
        {
            if (disposed) throw new ObjectDisposedException(nameof(SimUdpConnection));
            if (bytes == null || port < 1 || port > 65535) throw new ArgumentException("Invalid UDP destination or payload.");
            client.Send(bytes, bytes.Length, host, port);
        }

        public void Dispose()
        {
            disposed = true;
            client.Close();
            while (queue.TryDequeue(out _)) Interlocked.Decrement(ref queued);
        }
    }
}
