using System;

namespace ProtoFact.Engine
{
    public static class MathUtil
    {
        public const double Epsilon = 1e-6;

        public static bool LessThan(double a, double b)
            => a < b - Epsilon;

        public static bool GreaterOrEqual(double a, double b)
            => a >= b - Epsilon;

        public static double Normalize(double value)
        {
            return Math.Abs(value) < Epsilon ? 0 : value;
        }
    }
}