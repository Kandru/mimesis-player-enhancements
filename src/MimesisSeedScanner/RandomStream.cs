namespace MimesisSeedScanner
{
    /// <summary>
    /// Port of DunGen.RandomStream — must match game PRNG for seed parity.
    /// </summary>
    public sealed class RandomStream
    {
        private const int SeedConstant = 161803398;

        private int _iNext;
        private int _iNextP;
        private readonly int[] _seedArray = new int[56];

        public RandomStream()
            : this(Environment.TickCount)
        {
        }

        public RandomStream(int seed)
        {
            int num = SeedConstant - (seed == int.MinValue ? int.MaxValue : Math.Abs(seed));
            _seedArray[55] = num;
            int num2 = 1;
            for (int i = 1; i < 55; i++)
            {
                int num3 = 21 * i % 55;
                _seedArray[num3] = num2;
                num2 = num - num2;
                if (num2 < 0)
                {
                    num2 += int.MaxValue;
                }

                num = _seedArray[num3];
            }

            for (int j = 1; j < 5; j++)
            {
                for (int k = 1; k < 56; k++)
                {
                    _seedArray[k] -= _seedArray[1 + (k + 30) % 55];
                    if (_seedArray[k] < 0)
                    {
                        _seedArray[k] += int.MaxValue;
                    }
                }
            }

            _iNext = 0;
            _iNextP = 21;
        }

        public int Next() => InternalSample();

        public int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return (int)(Sample() * maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            long range = maxValue - (long)minValue;
            if (range <= int.MaxValue)
            {
                return (int)(Sample() * range) + minValue;
            }

            return (int)((long)(GetSampleForLargeRange() * range) + minValue);
        }

        public double NextDouble() => Sample();

        private double Sample() => InternalSample() * 4.656612875245797E-10;

        private double GetSampleForLargeRange()
        {
            int num = InternalSample();
            if (InternalSample() % 2 == 0)
            {
                num = -num;
            }

            return (num + 2147483646.0) / 4294967293.0;
        }

        private int InternalSample()
        {
            int num = _iNext;
            int num2 = _iNextP;
            if (++num >= 56)
            {
                num = 1;
            }

            if (++num2 >= 56)
            {
                num2 = 1;
            }

            int num3 = _seedArray[num] - _seedArray[num2];
            if (num3 == int.MaxValue)
            {
                num3--;
            }

            if (num3 < 0)
            {
                num3 += int.MaxValue;
            }

            _seedArray[num] = num3;
            _iNext = num;
            _iNextP = num2;
            return num3;
        }
    }
}
