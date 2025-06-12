using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace iM
{
    /// <summary>
    /// Block for a UDP input from Unity's Streamlined Input Manager (or similar)
    /// To be as generic as possible the following syntax has been implemented
    /// |0: How many numbers | 1: value type | 2: data type | 3: data subtype | 4 => 11: timestamp | 12 => -3: DATA | -2: => -1: Counter (as uInt16)|
    /// 
    /// These combinations need to be defined in json / yaml (for now in struct below)
    /// [1]: 
    /// [2,3]:
    ///       "0" EMG - "0.0" raw EMG, "0.1" ARVs ... tbd
    ///       "1" PositionControl - "1.0" th_flex, "1.1" th_rot, "1.2" index, "1.3" middle, "1.4" ring, "1.5" little, "1.6" wr_flex_ext, "1.7" wr_uln_rad, "1.8" wr_sup_pron, ...
    ///       "2" "FMG" - "2.0" raw FMG
    ///       "tbd"...
    ///       "3" "PositionControl2" - hack for bimanual see above
    /// </summary>
    public class UDP_StreamlinedRecvManager
    {
        public static UDP_StreamlinedRecvManager Instance = null;
        public static readonly object _lock = new object();

        private bool initialised;
        private string ipAdress;
        private int port;

        // receiving Thread
        Thread receiveThread;

        // udpclient object
        UdpClient client;

        // Define event
        // Define a delegate for the event
        public delegate void MessageEventHandler(UdpObject udpObject);
        // Declare the event using the delegate
        public event MessageEventHandler MessageSent;
        IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

        public UDP_StreamlinedRecvManager(string ipAdress, int port)
        {
            if (Instance == null)
            {
                lock (_lock)
                {
                    client = new UdpClient(port);
                    //receiveThread = new Thread(
                    //    new ThreadStart(StartUDPlistener));
                    //receiveThread.IsBackground = true;
                    //receiveThread.Start();

                    StartUDPlistener();

                    // this starts recursive(!) async loop
                    // client.BeginReceive(new AsyncCallback(recv), null);
                }
            }
            else
            {
                throw new Exception("There is already an Instance of the StreamlinedRecv");
            }
            
        }

        static public void TryCreateGlobalReceiver(string ipAdress, int port)
        {
            if (Instance == null)
            {
                Instance = new UDP_StreamlinedRecvManager(ipAdress, port);
            }
            else
            {
                Console.WriteLine($"Global Receiver is already running with ip {Instance.ipAdress} and port {Instance.port}");
            }
        }

        private async Task StartUDPlistener()
        {
            await Task.Delay(2000);
            try
            {
                // this starts recursive(!) async loop
                client.BeginReceive(new AsyncCallback(recv), null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

        }

        void recv(IAsyncResult res)
        {
            // receives from any ip and any port, which send to this port 11000 (as this is only receiving, no need to specify sender for now)
            //IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

            // client will be null after stopping the scene
            if (client != null)
            {
                byte[] received = client.EndReceive(res, ref RemoteIpEndPoint);

                // watch out for next sample
                client.BeginReceive(new AsyncCallback(recv), null);

                UdpObject receivedUdpObject = new UdpObject(received);

                // Send event to all StreamlinedReceivers so that they can check
                MessageSent?.Invoke(receivedUdpObject);
            }

        }

    }

    public class UdpObject
    {
        private int dataPointSize;
        public byte category;
        public byte subCategory;
        public double timestamp;
        public byte[] data;
        public UInt16 counter;
        public Type dtype;

        public UdpObject(byte[] received)
        {
            category = received[2];
            subCategory = received[3];
            dtype = getType(received[1]);
            
            // fix ts later
            timestamp = 0.0;
            //timestamp = getTimestamp(new ArraySegment<byte>(received, 4, 8));

            counter = BitConverter.ToUInt16(received, received.Length - 2);
            dataPointSize = Marshal.SizeOf(dtype);

            // Copy the received data part of the message into the data field of the UdpObject
            data = new byte[received[0] * Marshal.SizeOf(dtype)];
            Array.Copy(received, 12, data, 0, data.Length);
        }

        public object[] Data
        {
            get
            {
                object[] ret = new object[data.Length / dataPointSize];
                if (dtype == typeof(float))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToSingle(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(double))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToDouble(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(sbyte))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = unchecked((sbyte)data[i]);
                    }
                    return ret;
                }
                else if (dtype == typeof(short))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToInt16(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(int))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToInt32(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(long))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToInt64(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(byte))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = data[i];
                    }
                    return ret;
                }
                else if (dtype == typeof(ushort))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToUInt16(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(uint))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToUInt32(data, i * dataPointSize);
                    }
                    return ret;
                }
                else if (dtype == typeof(ulong))
                {
                    for (int i = 0; i < ret.Length; i++)
                    {
                        ret[i] = BitConverter.ToUInt64(data, i * dataPointSize);
                    }
                    return ret;
                }
                else
                {
                    throw new InvalidOperationException("UDP data type (defined in second sent byte) not compatible! Needs to be one of [float, double, long, ulong, int, uint, short, ushort, byte, sbyte]!");
                }
            }
        }

        // timestamp as double
        //private static double getTimestamp(ArraySegment<byte> byteTime)
        //{
        //    return BitConverter.ToDouble(byteTime);
        //}

        // deetrmine type of the values sent in an udp message
        private static Type getType(byte typeInfo)
        {
            // float vs integer
            if ((typeInfo & 0b_0010_0000) != 0)
            {
                // floating point
                // num bytes
                switch (typeInfo & 0b_0000_1111)
                {
                    case 4:
                        return typeof(float);
                    case 8:
                        return typeof(double);
                    default:
                        throw new InvalidOperationException("UDP data type (defined in second sent byte) not compatible! Needs to be one of [float, double, long, ulong, int, uint, short, ushort, byte, sbyte]!");
                }
            }
            // integer
            else
            {
                // signed vs. unsigned
                if ((typeInfo & 0b_0001_0000) != 0)
                {
                    // signed
                    // num bytes
                    switch (typeInfo & 0b_0000_1111)
                    {
                        case 1:
                            return typeof(sbyte);
                        case 2:
                            return typeof(short);
                        case 4:
                            return typeof(int);
                        case 8:
                            return typeof(long);
                        default:
                            throw new InvalidOperationException("UDP data type (defined in second sent byte) not compatible! Needs to be one of [float, double, long, ulong, int, uint, short, ushort, byte, sbyte]!");
                    }
                }
                // unsigned
                else
                {
                    // num bytes
                    switch (typeInfo & 0b_0000_1111)
                    {
                        case 1:
                            return typeof(byte);
                        case 2:
                            return typeof(ushort);
                        case 4:
                            return typeof(uint);
                        case 8:
                            return typeof(ulong);
                        default:
                            throw new InvalidOperationException("UDP data type (defined in second sent byte) not compatible! Needs to be one of [float, double, long, ulong, int, uint, short, ushort, byte, sbyte]!");
                    }

                }
            }
        }
    }


}
