using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mmc_map_finder2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Can't find the file by path: " + args[0]);
                return;
            }

            List<Pattern> patterns = new List<Pattern>()
            { 
                new Pattern()
                {
                    Header = new byte[] { 0x02, 0x00 },
                    HeaderLenght = 4,
                    PayloadLenght = 8
                }
            };

            using (FileStream fs = new FileStream(args[0], FileMode.Open))
            using (StreamWriter sw = new StreamWriter(args[0] + ".map"))
            {
                fs.Seek(0x2a00, SeekOrigin.Begin);

                Queue<byte> queue = new Queue<byte>();
                int queueLenght = patterns.Select(p => p.HeaderLenght + p.PayloadLenght + p.HeaderLenght).Max();

                while (fs.Position < fs.Length)
                {
                    queue.Enqueue((byte)fs.ReadByte());

                    if (queue.Count >= queueLenght)
                    {
                        byte[] buffer = queue.ToArray();

                        foreach (var pattern in patterns)
                        {
                            byte[] startheaderPattern = SubArray(buffer, 0, pattern.Header.Length);
                            byte[] endheaderPattern = SubArray(buffer, pattern.HeaderLenght + pattern.PayloadLenght, pattern.Header.Length);

                            if (CompareArrays(startheaderPattern, pattern.Header) && CompareArrays(endheaderPattern, pattern.Header))
                            {
                                byte[] payload = SubArray(buffer, pattern.HeaderLenght, pattern.PayloadLenght);
                                sw.WriteLine(FormattedData(fs.Position - queueLenght + pattern.HeaderLenght, payload));
                            }
                        }

                        queue.Dequeue();
                    }
                }
            }
        }

        static T[] SubArray<T>(T[] array, int start, int len)
        {
            T[] buffer = new T[len];

            for (int i = 0; i < len; i++)
                buffer[i] = array[start + i];

            return buffer;
        }

        static string FormattedData(long address, byte[] value)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < value.Length; i++)
            {
                sb.Append(string.Format(" {0:000} |", value[i]));
            }

            return string.Format("0x{0:x}: {1}", address, sb.ToString());
        }

        static string FormattedData(long address, double value)
        {
            double min = 0;
            double max = 256;
            int size = (int)((value / (max - min)) * 100);
            char[] buffer = new char[size];
            for (int i = 0; i < size; i++)
            {
                buffer[i] = '*';
            }

            return string.Format("0x{0:x}: {1:F} {2}", address, value, new String(buffer));
        }

        static double EGRDuty(FileStream fs)
        {
            double x = ReadDataUnsigned(fs, 1);

            return x / 1.28;
        }

        static double CrankPulseTime(double x)
        {
            return x;
        }

        static double Timing(double x)
        {
            return x;
        }

        static bool CompareArrays(byte[] a, byte[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }

        static double CompareArrays(double[] a, double[] b)
        {
            double total = 0;

            for (int i = 0; i < a.Length; i++)
            {
                double error = 0;

                double _a = Math.Abs(a[i]);
                double _b = Math.Abs(b[i]);

                if (_a > _b)
                {
                    error = _a / _b;
                }
                else
                {
                    error = _b / _a;
                }

                total += error > 2 ? 2 : error;
            }

            return total / a.Length;
        }

        static double ReadDataUnsigned(Stream fs, int byteCount)
        {
            byte[] buffer = new byte[byteCount];
            fs.Read(buffer, 0, buffer.Length);

            double realValue = 0;

            if (byteCount == 1)
            {
                realValue = (double)buffer[0];
            }
            else
            {
                realValue = (double)BitConverter.ToUInt16(Reverse(buffer), 0);
            }

            return realValue;
        }

        static double ReadDataSigned(Stream fs, int byteCount)
        {
            byte[] buffer = new byte[byteCount];
            fs.Read(buffer, 0, buffer.Length);

            double realValue = 0;

            if (byteCount == 1)
            {
                realValue = (double)(sbyte)buffer[0];
            }
            else
            {
                realValue = (double)BitConverter.ToInt16(Reverse(buffer), 0);
            }

            return realValue;
        }

        static byte[] Reverse(byte[] array)
        {
            for (int i = 0; i < array.Length / 2; i++)
            {
                byte b = array[i];
                array[i] = array[array.Length - 1 - i];
                array[array.Length - 1 - i] = b;
            }

            return array;
        }

        static double TimeTCOMPCrankingHack(double x)
        {
            return x * 0.9;
        }

        static double Temp(double x)
        {
            return x - 40;
        }

        static double AFR(double x)
        {
            if (x != 0)
                return 14.7 * (128.0 / x);
            else
                return 0;
        }

        static double RPM(double x)
        {
            return x * 1000.0 / 256.0;
        }

        static double InjectorScaling(double x)
        {
            return 29241.0 / x;
        }

        static double BatteryVoltage(double x)
        {
            return x * 75.0 / 1024.0;
        }

        static double InjectorLatency(double x)
        {
            return x * 0.024;
        }

        static double Load(double x)
        {
            return (x * 10) / 32;
        }

        static double TempScale(FileStream fs)
        {
            double x = ReadDataSigned(fs, 1);
            return x;
        }
        static double VoltsADCx4(FileStream fs)
        {
            double x = ReadDataSigned(fs, 2);
            return (x * 5) / 1023;
        }
        static double ChargeTime(FileStream fs)
        {
            double x = ReadDataUnsigned(fs, 1);
            return x * 0.064;
        }
        static double EnrichmentAdj(FileStream fs)
        {
            double x = ReadDataUnsigned(fs, 1);
            return x / 128;
        }
        static double AccelEnrichBase(FileStream fs)
        {
            double x = ReadDataUnsigned(fs, 1);
            return x;
        }
        static double Percent256(FileStream fs)
        {
            double x = ReadDataUnsigned(fs, 1);
            return x / 2.55;
        }
    }
}
