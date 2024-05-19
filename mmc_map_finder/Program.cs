using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;

namespace mmc_map_finder
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Please specify two ROM bin files path");
                return;
            }

            if (!File.Exists(args[0]))
            {
                Console.WriteLine("Can't find the file by path: " + args[0]);
                return;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine("Can't find the file by path: " + args[1]);
                return;
            }

            int size = 8;
            double[] testDataSet = new double[] {
              117,
              91,
              70,
              52,
              41,
              28,
              12,
              9
            };



            //for (int i = 0; i < size; i++)
            //{
            //    testDataSet[i] = 10 * (i + 1);
            //}

            //using (FileStream testDataSetfs = new FileStream(args[0], FileMode.Open))
            //using (StreamWriter sw = new StreamWriter(args[0] + ".map"))
            //{
                
            //    testDataSetfs.Position = 0x2e35;

            //    for (int i = 0; i < size; i++)
            //    {
            //        testDataSet[i] = EGRDuty(testDataSetfs);
            //    }

            //    //while (testDataSetfs.Position < testDataSetfs.Length)
            //    //{
            //    //    double realValue = 0;

            //    //    realValue = AFR(ReadData(testDataSetfs, 1));

            //    //    sw.WriteLine(FormattedData(testDataSetfs.Position, realValue.ToString("F")));
            //    //}

            //}

            Queue<double> queue = new Queue<double>();

            using (FileStream fs = new FileStream(args[1], FileMode.Open))
            using (StreamWriter sw = new StreamWriter(args[1] + ".map"))
            {
                while (fs.Position < fs.Length)
                {
                    /*
                     * Calculation func
                     */
                    double realValue = Percent256(fs);

                    queue.Enqueue(realValue);

                    if (queue.Count > size + 3)
                    {
                        queue.Dequeue();

                        double[] buffer = queue.ToArray();

                        if ((buffer[0] == 0) && (buffer[size + 1] <= 0.02) && (buffer[size + 2] == 0))
                        {
                            double res = CompareArrays(SubArray(buffer, 1, size), testDataSet);

                            if (res <= 1.4)
                                Console.WriteLine(string.Format("0x{0:x}", fs.Position - (testDataSet.Length - 1)));
                        }
                    }

                    sw.WriteLine(FormattedData(fs.Position - 1, realValue));
                }
            }

            Console.WriteLine("Finish");
            Console.ReadKey();
        }

        static T[] SubArray<T>(T[] array, int start, int len)
        {
            T[] buffer = new T[len];

            for(int i=0; i < len; i++)
                buffer[i] = array[start + i];

            return buffer;
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
