using System;
using System.Collections.Generic;

namespace Sim
{
    /// <summary>Bounded, per-channel packet history. Zero samples back means newest.</summary>
    public sealed class SimHistory
    {
        readonly Dictionary<int, List<SimPacket>> channels = new Dictionary<int, List<SimPacket>>();
        public int Capacity { get; set; } = 100;
        public double LastTimestampSeconds { get; private set; } = -1;

        // Call on one owning thread, typically the Unity main thread after draining UDP.
        public void Add(SimPacket packet)
        {
            int key = packet.Category | packet.Subcategory << 8;
            if (!channels.TryGetValue(key, out var history)) channels.Add(key, history = new List<SimPacket>());
            history.Add(packet);
            int excess = history.Count - Math.Max(1, Capacity);
            if (excess > 0) history.RemoveRange(0, excess);
            LastTimestampSeconds = packet.TimestampSeconds;
        }

        public SimPacket Get(byte category, byte subcategory, int samplesBack = 0, double secondsBack = 0)
        {
            if (samplesBack < 0 || secondsBack < 0 || double.IsNaN(secondsBack) || double.IsInfinity(secondsBack) ||
                (samplesBack != 0 && secondsBack != 0)) throw new ArgumentException("Choose one nonnegative delay.");
            if (!channels.TryGetValue(category | subcategory << 8, out var history)) return null;
            if (samplesBack >= history.Count) return null;
            if (secondsBack == 0) return history[history.Count - 1 - samplesBack];
            double target = history[history.Count - 1].TimestampSeconds - secondsBack;
            SimPacket best = null;
            double error = double.MaxValue;
            foreach (var item in history)
            {
                double difference = Math.Abs(item.TimestampSeconds - target);
                if (difference < error) { best = item; error = difference; }
            }
            // No silently shortened time delay when the requested history is unavailable.
            double oldest = double.MaxValue;
            foreach (var item in history) oldest = Math.Min(oldest, item.TimestampSeconds);
            return target < oldest ? null : best;
        }
        /// <summary>Read a channel batch; optional draining is useful for recorded feature streams.</summary>
        public SimPacket[] ReadAll(byte category, byte subcategory, bool clear = false)
        {
            int key = category | subcategory << 8;
            if (!channels.TryGetValue(key, out var history)) return null;
            var result = history.ToArray();
            if (clear) channels.Remove(key);
            return result;
        }
        public void Clear() { channels.Clear(); LastTimestampSeconds = -1; }
    }
}
