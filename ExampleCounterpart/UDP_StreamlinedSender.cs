using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace iM
{
    /// <summary>
    /// Block for a UDP output for Unity's Streamlined Input Manager
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
    public class UDP_StreamlinedSender : Block
    {
        /// <summary>
        /// Udp client for the connection with e.g. Unity
        /// </summary>
        private UdpClient _UdpClient = new UdpClient();

        /// <summary>
        /// Dict of additional senders as defined in YAML. Key is the Block name (not type). Value is the tuple for the data category (arbitrary see examples above)
        /// </summary>
        private Dictionary<string, (byte, byte)> AdditionalSenders = new Dictionary<string, (byte, byte)>();

        /// <summary>
        /// Arrays of supported data types for type checks
        /// </summary>
        private readonly Type[] floatingTypes = { typeof(float), typeof(double) };
        private readonly Type[] signedTypes = { typeof(sbyte), typeof(short), typeof(int), typeof(long) };
        private readonly Type[] unsignedTypes = { typeof(ushort), typeof(uint), typeof(ulong), typeof(byte) };

        public UDP_StreamlinedSender(string Name, double DesiredRate, List<string> InputCfg, List<string> Params, string Path)
            : base(Name, DesiredRate, InputCfg, Params, Path)
        {

        }

        override public void ConfigureInputs()
        {
            base.ConfigureInputs();

            if (Params.Count < 2)
            {
                throw new Exception($"Block {Name} must have at least 2 parameters [hostIP, Port]");
            }

            // open UDP stream client!
            _UdpClient.Connect(Params[0], Convert.ToInt32(Params[1]));

            // if additional senders are defined "joiner:0.0" split them by ':' and '.'
            if (Params.Count > 2)
            {
                // loop over additional params
                for (int i = 2; i < Params.Count; i++)
                {
                    // check for splitting chars
                    if (Params[i].Contains(":") && Params[i].Contains("."))
                    {
                        // split by splitting chars
                        string[] split1 = Params[i].Split(':');
                        string[] split2 = split1[1].Split('.');
                        if (split1.Length != 2 && split2.Length != 2) throw new Exception("Name and category are not correctly added to yaml");

                        // get key and val and add to dictionary
                        string key = split1[0];
                        (byte, byte) val = (byte.Parse(split2[0]), byte.Parse(split2[1]));
                        AdditionalSenders.Add(key, val);
                    }
                }
            }
        }

        override protected void OnNewInput(Block sender, dynamic value)
        {
            List<byte> dataList = new List<byte>();

            // Special case: When signals come from Control Algorithm => Dictionary based on Degrees of Actuation from CA block
            if (sender.GetType().Name == "ControlAlgorithm")
            {
                Dictionary<DOAs, double> actuationDict = value as Dictionary<DOAs, double>;

                foreach (var item in actuationDict)
                {
                    // new data list
                    dataList = new List<byte>();

                    // with DoAs only one number is streamed at a time
                    byte numCount = 1;

                    // Category: 1, Subcategory: DOA-Enum
                    (byte, byte) dataType = (1, (byte)item.Key);

                    // hack for bimanual interaction 
                    //if (sender.Name == "controlAlgorithm2")
                    //{
                    //    // Category: 3, Subcategory: DOA-Enum
                    //    dataType = ( 3, (byte)item.Key );
                    //}

                    // add value
                    dataList.AddRange(BitConverter.GetBytes(item.Value));

                    // send out UDP message
                    SendUDPmessage(numCount, typeof(double), dataType, dataList.ToArray());
                }
            }

            // Historically wanted bytes instead of doubles, because this was the real range of myos and saves computation and bandwidth
            // When signal come from Myo => Incoming doubles get "returned" to bytes 
            else if (sender.GetType().Name == "Myo")
            {
                Vector valVec = value as Vector;
                (byte, byte) dataType = (0, 0);

                // Special case as these "doubles" are actually sbytes divided by 128 => can be send as bytes for performance reasons
                for (int i = 0; i < valVec.Count; i++) dataList.Add((byte)(128 * valVec[i])); // needs to be interpreted as sByte when decoding

                // send standardized UDP message
                SendUDPmessage((byte)dataList.Count, typeof(sbyte), dataType, dataList.ToArray());
            }

            // Cover all other senders which are defined in yaml file and added to "AdditionalSenders"
            else
            {
                if (AdditionalSenders.ContainsKey(sender.Name))
                {
                    Vector valVec = value as Vector;

                    // get the corresponding value
                    for (int i = 0; i < valVec.Count; i++)
                    {
                        // needs add range as it might add more then one byte
                        dataList.AddRange(BitConverter.GetBytes(valVec[i]));
                    }
                    // send standardized UDP message
                    SendUDPmessage((byte)valVec.Count(), typeof(double), AdditionalSenders[sender.Name], dataList.ToArray());
                }
            }
        }


        /// <summary>
        /// Function to get the specific command for UDP Protocol to hand over info on data type
        /// </summary>
        /// <param name="type">is a data type e.g. float </param>
        /// <returns></returns>
        private byte GetByteForType(Type type)
        {
            if (!floatingTypes.Contains(type) && !signedTypes.Contains(type) && !unsignedTypes.Contains(type))
            {
                throw new Exception("This data type is not support yet by the UDP protocol: " + type.Name);
            }

            byte typeInfo = 0b_0000_0000;

            // decimal?
            if (floatingTypes.Contains(type))
            {
                typeInfo += 0b_0010_0000;
            }
            // if not check if signed or unsigned (only positive)
            else if (signedTypes.Contains(type))
            {
                typeInfo += 0b_0001_0000;
            }
            // num of bytes?
            typeInfo += (byte)Marshal.SizeOf(type);

            return typeInfo;
        }
        /// <summary>
        /// This is the general UDP counter of sent messages over all types
        /// </summary>
        UInt16 UDP_counter = 0;

        /// <summary>
        /// This function let's you send out standardized UDP messages
        /// </summary>
        /// <param name="numCount">how many numbers (not how many bytes)</param>
        /// <param name="valType">what kind of value type (double, float, int32 etc)</param>
        /// <param name="dataType">What kind of data of struct Data_Type are you sending</param>
        /// <param name="data">Actual data as byte array</param>
        private void SendUDPmessage(byte numCount, Type valType, (byte, byte) dataType, byte[] data)
        {
            // add components
            List<byte> udpMessage = new List<byte>
            {
                numCount,
                GetByteForType(valType),
                dataType.Item1,
                dataType.Item2,
            };
            udpMessage.AddRange(BitConverter.GetBytes(Utils.Now));
            udpMessage.AddRange(data);
            udpMessage.AddRange(BitConverter.GetBytes(UDP_counter));

            UDP_counter++;

            _UdpClient.SendAsync(udpMessage.ToArray(), udpMessage.Count);
        }
    }
}
