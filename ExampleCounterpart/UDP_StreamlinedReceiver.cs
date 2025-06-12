using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Policy;
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
    public class UDP_SIM_Rec : Block
    {
        int category;
        int subcategory;

        public UDP_SIM_Rec(string Name, double DesiredRate, List<string> InputCfg, List<string> Params, string Path)
            : base(Name, DesiredRate, InputCfg, Params, Path)
        {
            if (Params.Count < 4)
            {
                throw new Exception($"Block {Name} must have 4 parameters [hostIP, Port, Category, Subcategory]");
            }
            // try to create global instance
            UDP_StreamlinedRecvManager.TryCreateGlobalReceiver(Params[0], int.Parse(Params[1]));
            Console.WriteLine($"Instance in Subscriber {Name}: {UDP_StreamlinedRecvManager.Instance.GetHashCode()}");

            // set category and subcategory
            category = int.Parse(Params[2]);
            subcategory = int.Parse(Params[3]);

            // listen to events from manager
            UDP_StreamlinedRecvManager.Instance.MessageSent += OnUdpObjReceived;
        }

        override public void ConfigureInputs()
        {
            base.ConfigureInputs();


        }

        private void OnUdpObjReceived(UdpObject udpObject)
        {
            lock (UDP_StreamlinedRecvManager._lock)
            {
                // check if it is from the correct cat & subcategory
                if (udpObject.category == category && udpObject.subCategory == subcategory)
                {
                    if (udpObject.dtype == typeof(double))
                    {
                        double[] data = udpObject.Data.Cast<double>().ToArray();
                        Vector val = Vector.Build.DenseOfArray(data);

                        SendOutput(val);
                    }
                }
            } 
        }

        override protected void OnNewInput(Block sender, dynamic value)
        {
            
        }

    }
}
