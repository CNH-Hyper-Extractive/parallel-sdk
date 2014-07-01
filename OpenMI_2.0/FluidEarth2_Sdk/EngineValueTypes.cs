using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FluidEarth2.Sdk
{
    public static class EngineValueTypes
    {
        public interface IEngineType { }

        public struct Int32 : IEngineType
        {
            public System.Int32 Value;

            public Int32(System.Int32 v)
            {
                Value = v;
            }

            public Int32(string value)
                : this(Convert.ToInt32(value))
            { }

            public override string ToString()
            {
                return Convert.ToString(Value);
            }

            public static IEnumerable<System.Int32> ToInt32s(IEnumerable<Int32> from)
            {
                return from.Select(v => v.Value);
            }

            public static IEnumerable<Int32> FromInt32s(IEnumerable<System.Int32> from)
            {
                return from.Select(v => new Int32(v));
            }

            public static string ToCsv(IEnumerable<Int32> from)
            {
                return from
                    .Select(c => c.ToString())
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<Int32> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new Int32(v))
                    .ToArray();
            }

            public static IEnumerable<Int32> LinearInterpolation(IEnumerable<Int32> below, IEnumerable<Int32> above, double factor)
            {
                throw new Exception("Cannot perform linear interpolation on EngineValueTypes.Int32 type");
            }
        }

        public struct Boolean : IEngineType
        {
            public bool Value;

            public Boolean(bool v)
            {
                Value = v;
            }

            public Boolean(string value)
                : this(Convert.ToBoolean(value))
            { }

            public override string ToString()
            {
                return Convert.ToString(Value);
            }

            public static IEnumerable<bool> ToBools(IEnumerable<Boolean> from)
            {
                return from.Select(v => v.Value);
            }

            public static IEnumerable<Boolean> FromBools(IEnumerable<bool> from)
            {
                return from.Select(v => new Boolean(v));
            }

            public static string ToCsv(IEnumerable<Boolean> from)
            {
                return from
                    .Select(c => c.ToString())
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<Boolean> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new Boolean(v))
                    .ToArray();
            }

            public static IEnumerable<Boolean> LinearInterpolation(IEnumerable<Boolean> below, IEnumerable<Boolean> above, double factor)
            {
                throw new Exception("Cannot perform linear interpolation on EngineValueTypes.Boolean type");
            }
        }

        public struct Double : IEngineType
        {
            public double Value;

            public Double(double v)
            {
                Value = v;
            }

            public Double(string value)
                : this(Convert.ToDouble(value))
            { }

            public override string ToString()
            {
                return Convert.ToString(Value);
            }

            public static IEnumerable<double> ToDoubles(IEnumerable<Double> from)
            {
                return from.Select(v => v.Value);
            }

            public static IEnumerable<Double> FromDoubles(IEnumerable<double> from)
            {
                return from.Select(v => new Double(v));
            }

            public static string ToCsv(IEnumerable<Double> from)
            {
                return from
                    .Select(c => c.ToString())
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<Double> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new Double(v))
                    .ToArray();
            }

            public static IEnumerable<Double> LinearInterpolation(IEnumerable<Double> below, IEnumerable<Double> above, double factor)
            {
               var a = ToDoubles(above).ToArray();

               return FromDoubles(
                   ToDoubles(below)
                    .Select((b, n) => b + factor * (a[n] - b)));
            }
        }

        public struct Double2d : IEngineType
        {
            public double Value1;
            public double Value2;

            public Double2d(double v1, double v2)
            {
                Value1 = v1;
                Value2 = v2;
            }

            public Double2d(string csv)
            {
                var parts = csv.Split(' ');
                Value1 = Convert.ToDouble(parts[0]);
                Value2 = Convert.ToDouble(parts[1]);
            }

            public override string ToString()
            {
                return string.Format("{0} {1}", 
                    Convert.ToString(Value1), Convert.ToString(Value2));
            }

            public static double[] ToDoubles(IEnumerable<Double2d> from)
            {
                var values = new double[from.Count() * 2];

                int n = -1;

                foreach (var value in from)
                {
                    values[++n] = value.Value1;
                    values[++n] = value.Value2;
                }

                return values;
            }

            public static Double2d[] FromDoubles(double[] from)
            {
                int m = from.Count() / 2;
                var values = new Double2d[m];

                int n2;
                for (int n = 0; n < m; ++n)
                {
                    n2 = 2 * n;
                    values[n] = new Double2d(from[n2], from[++n2]);
                }

                return values;
            }

            public static string ToCsv(IEnumerable<Double2d> from)
            {
                return from
                    .Select(c => c.ToString())
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<Double2d> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new Double2d(v));
            }

            public static Double2d[] LinearInterpolation(IEnumerable<Double2d> below, IEnumerable<Double2d> above, double factor)
            {
                var a = ToDoubles(above).ToArray();

                return FromDoubles(
                    ToDoubles(below)
                    .Select((b, n) => b + factor * (a[n] - b))
                    .ToArray());
            }
        }

        public struct Double3d : IEngineType
        {
            public double Value1;
            public double Value2;
            public double Value3;

            public Double3d(double v1, double v2, double v3)
            {
                Value1 = v1;
                Value2 = v2;
                Value3 = v3;
            }

            public Double3d(string csv)
            {
                var parts = csv.Split(' ');
                Value1 = Convert.ToDouble(parts[0]);
                Value2 = Convert.ToDouble(parts[1]);
                Value3 = Convert.ToDouble(parts[2]);
            }

            public override string ToString()
            {
                return string.Format("{0} {1} {2}",
                    Convert.ToString(Value1), Convert.ToString(Value2), Convert.ToString(Value3));
            }

            public static double[] ToDoubles(IEnumerable<Double3d> from)
            {
                var values = new double[from.Count() * 3];

                int n = -1;

                foreach (var value in from)
                {
                    values[++n] = value.Value1;
                    values[++n] = value.Value2;
                    values[++n] = value.Value3;
                }

                return values;
            }

            public static Double3d[] FromDoubles(double[] from)
            {
                int m = from.Count() / 3;
                var values = new Double3d[m];

                int n3;
                for (int n = 0; n < m; ++n)
                {
                    n3 = 3 * n;
                    values[n] = new Double3d(from[n3], from[++n3], from[++n3]);
                }

                return values;
            }

            public static string ToCsv(IEnumerable<Double3d> from)
            {
                return from
                    .Select(c => c.ToString())
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<Double3d> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new Double3d(v));
            }

            public static Double3d[] LinearInterpolation(IEnumerable<Double3d> below, IEnumerable<Double3d> above, double factor)
            {
                var a = ToDoubles(above).ToArray();

                return FromDoubles(
                    ToDoubles(below)
                    .Select((b, n) => b + factor * (a[n] - b))
                    .ToArray());
            }
        }

        public class String
        {
            public string Value;

            public String()
                : this("")
            { }

            public String(string v)
            {
                Value = v;
            }

            public static IEnumerable<string> ToStrings(IEnumerable<String> from)
            {
                return from.Select(v => v.Value);
            }

            public static IEnumerable<String> FromStrings(IEnumerable<string> from)
            {
                return from.Select(v => new String(v));
            }

            public static string ToCsv(IEnumerable<String> from)
            {
                return from
                    .Select(c => c.Value.Replace(',', '~'))
                    .Aggregate(new StringBuilder(), (sb, d) => sb.Append(d + ","))
                    .ToString();
            }

            public static IEnumerable<String> FromCsv(string from)
            {
                return from
                    .Split(',')
                    .Select(v => new String(v.Replace('~',',')))
                    .ToArray();
            }

            public static IEnumerable<String> LinearInterpolation(IEnumerable<String> below, IEnumerable<String> above, double factor)
            {
                throw new Exception("Cannot perform linear interpolation on EngineValueTypes.String type");
            }
        }
    }
}
