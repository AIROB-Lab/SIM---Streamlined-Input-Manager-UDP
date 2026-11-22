using System;
using System.IO;

namespace Sim
{
    /// <summary>A decoded SIM datagram. Values retain their declared numeric type.</summary>
    public sealed class SimPacket
    {
        public byte Category { get; }
        public byte Subcategory { get; }
        public double TimestampSeconds { get; }
        public ushort Counter { get; }
        public double ReceivedAtSeconds { get; internal set; }
        public object[] Values { get; }

        internal SimPacket(byte category, byte subcategory, double timestamp, ushort counter, object[] values)
        {
            Category = category;
            Subcategory = subcategory;
            TimestampSeconds = timestamp;
            Counter = counter;
            Values = values;
        }
    }

    /// <summary>SIM's little-endian wire format, independent of Unity, devices and sockets.</summary>
    public static class SimPacketCodec
    {
        // count, type, category, subcategory, double timestamp, payload, ushort counter.
        public const int EnvelopeBytes = 14;

        public static bool TryDecode(byte[] bytes, out SimPacket packet)
        {
            packet = null;
            if (bytes == null || bytes.Length < EnvelopeBytes) return false;
            int count = bytes[0], type = bytes[1], width = type & 15;
            bool supported = type == 0x24 || type == 0x28 || type == 0x11 ||
                type == 0x12 || type == 0x14 || type == 0x18 ||
                type == 1 || type == 2 || type == 4 || type == 8;
            if (!supported || count == 0 || bytes.Length != EnvelopeBytes + count * width) return false;
            using (var reader = new BinaryReader(new MemoryStream(bytes, false)))
            {
                reader.ReadBytes(4);
                double time = reader.ReadDouble();
                if (double.IsNaN(time) || double.IsInfinity(time)) return false;
                var values = new object[count];
                for (int i = 0; i < count; i++)
                {
                    // Explicit branches preserve boxed numeric types, including ulong precision.
                    switch (type)
                    {
                        case 0x24: values[i] = reader.ReadSingle(); break;
                        case 0x28: values[i] = reader.ReadDouble(); break;
                        case 0x11: values[i] = reader.ReadSByte(); break;
                        case 0x12: values[i] = reader.ReadInt16(); break;
                        case 0x14: values[i] = reader.ReadInt32(); break;
                        case 0x18: values[i] = reader.ReadInt64(); break;
                        case 1: values[i] = reader.ReadByte(); break;
                        case 2: values[i] = reader.ReadUInt16(); break;
                        case 4: values[i] = reader.ReadUInt32(); break;
                        case 8: values[i] = reader.ReadUInt64(); break;
                    }
                    double value = Convert.ToDouble(values[i]);
                    if (double.IsNaN(value) || double.IsInfinity(value)) return false;
                }
                packet = new SimPacket(bytes[2], bytes[3], time, reader.ReadUInt16(), values);
                return true;
            }
        }

        /// <summary>Encode a nonempty double array, with the caller's source time and counter.</summary>
        public static byte[] Encode(double[] values, byte category, byte subcategory, double timestamp, ushort counter)
        {
            if (values == null || values.Length == 0 || values.Length > 255)
                throw new ArgumentException("SIM requires 1 to 255 values.", nameof(values));
            if (double.IsNaN(timestamp) || double.IsInfinity(timestamp)) throw new ArgumentException("Invalid timestamp.");
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((byte)values.Length); writer.Write((byte)0x28);
                writer.Write(category); writer.Write(subcategory); writer.Write(timestamp);
                foreach (double value in values)
                {
                    if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentException("Nonfinite SIM value.");
                    writer.Write(value);
                }
                writer.Write(counter);
                return stream.ToArray();
            }
        }
    }
}

