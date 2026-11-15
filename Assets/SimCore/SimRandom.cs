// PeachMe SimCore - pure C#, no UnityEngine references allowed in this folder.

namespace PeachMe.SimCore
{
    /// <summary>
    /// Deterministic PRNG (xorshift32). Same seed gives the same track on every
    /// device and platform. Never use UnityEngine.Random or System.Random in SimCore.
    /// </summary>
    public sealed class SimRandom
    {
        private uint _state;

        public SimRandom(uint seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : seed;
            // Warm up so nearby seeds diverge quickly.
            for (int i = 0; i < 8; i++) NextUInt();
        }

        public uint NextUInt()
        {
            uint x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x;
            return x;
        }

        /// <summary>Uniform float in [0, 1).</summary>
        public float NextFloat()
        {
            return (NextUInt() >> 8) * (1f / 16777216f);
        }

        public float Range(float min, float max)
        {
            return min + (max - min) * NextFloat();
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            uint span = (uint)(maxExclusive - minInclusive);
            return span == 0 ? minInclusive : minInclusive + (int)(NextUInt() % span);
        }

        public bool Chance(float p)
        {
            return NextFloat() < p;
        }

        public static uint Mix(uint a, uint b)
        {
            uint h = a ^ (b * 0x9E3779B1u);
            h ^= h >> 16;
            h *= 0x85EBCA6Bu;
            h ^= h >> 13;
            h *= 0xC2B2AE35u;
            h ^= h >> 16;
            return h;
        }
    }
}
