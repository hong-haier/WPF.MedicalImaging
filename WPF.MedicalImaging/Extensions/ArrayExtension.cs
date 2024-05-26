using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WPF.MedicalImaging.Extensions
{
    public static class ArrayExtension
    {
        #region 数组取指针和强制转换

        public static IntPtr GetIntPtr(this Array arr, int startIndex = 0)
        {
            return Marshal.UnsafeAddrOfPinnedArrayElement(arr, startIndex);
        }

        public static T[] As<T>(this Array array)
        {
            if (array.GetValue(0).GetType() != typeof(T)) throw new ArgumentException($"array is not {typeof(T)}");
            return (T[])array;
        }

        /// <summary>
        /// 将ImageData转成byte数组
        /// TODO: 注意，如果ImageData中数据大于255或小于0的话，得到的结果会和原始数据有偏差
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Array data)
        {
            object firstValue = data.GetValue(0);
            int length = data.Length;
            Type dataType = data.GetValue(0).GetType();
            var ptr = data.GetIntPtr(0);
            byte[] bytes = new byte[length];

            if (dataType == typeof(char) ||
                dataType == typeof(byte) ||
                dataType == typeof(sbyte))
            {
                Marshal.Copy(ptr, bytes, 0, length);
                return bytes;
            }

            Tuple<int, int>[] sesionsRange; // 分线程执行
            if (length < 10 * 1024 * 1024)
            {
                sesionsRange = new Tuple<int, int>[1] { new Tuple<int, int>(0, length) };
            }
            else
            {
                int sesions = 64; // 分成64个线程吧
                int sesionSize = length / sesions;
                sesionsRange = new Tuple<int, int>[sesions];
                for (int i = 0; i < sesions; i++)
                {
                    if (i == sesions - 1)
                        sesionsRange[i] = new Tuple<int, int>(i * sesionSize, length);
                    else
                        sesionsRange[i] = new Tuple<int, int>(i * sesionSize, (i + 1) * sesionSize);
                }
            }

            if (
                dataType == typeof(short) ||
                dataType == typeof(ushort) ||
                dataType == typeof(int) ||
                dataType == typeof(uint) ||
                dataType == typeof(long) ||
                dataType == typeof(ulong))
            {
                int dataTypeSize = dataType == typeof(short) || dataType == typeof(ushort) ? 2 :
                    dataType == typeof(int) || dataType == typeof(uint) ? 4 :
                    dataType == typeof(long) || dataType == typeof(ulong) ? 8 : 1;
                Parallel.ForEach(sesionsRange, sesion =>
                {
                    for (int i = sesion.Item1; i < sesion.Item2; i++)
                    {
                        bytes[i] = Marshal.ReadByte(ptr, i * dataTypeSize);
                    }
                });
            }
            else if (dataType == typeof(float))
            {
                float[] array = new float[length];
                Marshal.Copy(ptr, array, 0, length);
                Parallel.ForEach(sesionsRange, sesion =>
                {
                    for (int i = sesion.Item1; i < sesion.Item2; i++)
                    {
                        bytes[i] = (byte)array[i];
                    }
                });
            }
            else if (dataType == typeof(double))
            {
                double[] array = new double[length];
                Marshal.Copy(ptr, array, 0, length);
                Parallel.ForEach(sesionsRange, sesion =>
                {
                    for (int i = sesion.Item1; i < sesion.Item2; i++)
                    {
                        bytes[i] = (byte)array[i];
                    }
                });
            }
            return bytes;
        }

        #endregion

        #region 数组打印

        public static string ToDebugString(this IEnumerable<double> arr, int digits = 3)
        {
            return string.Join(",", arr.Select(x => Math.Round(x, digits)));
        }

        public static string ToDebugString(this IEnumerable<float> arr, int digits = 3)
        {
            return string.Join(",", arr.Select(x => Math.Round(x, digits)));
        }

        public static string ToDebugString(this IEnumerable<long> arr)
        {
            return string.Join(",", arr);
        }

        public static string ToDebugString(this IEnumerable<ulong> arr)
        {
            return string.Join(",", arr);
        }

        public static string ToDebugString(this IEnumerable<int> arr)
        {
            return string.Join(",", arr);
        }

        public static string ToDebugString(this IEnumerable<uint> arr)
        {
            return string.Join(",", arr);
        }

        public static string ToDebugString(this IEnumerable<short> arr)
        {
            return string.Join(",", arr);
        }

        public static string ToDebugString(this Array arr)
        {
            return string.Join(",", arr);
        }


        public static string ToDebugString<T>(this FellowOakDicom.DicomRange<T> range)
            where T : IComparable<T>
        {
            return $"{range.Minimum},{range.Maximum}";
        }

        #endregion

        #region 数组取值区间

        public static double[] Range(this IEnumerable<double> arr)
        {
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (var i in arr)
            {
                min = Math.Min(min, i);
                max = Math.Max(max, i);
            }
            return new double[] { min, max };
        }

        public static float[] Range(this IEnumerable<float> arr)
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            foreach (var i in arr)
            {
                min = Math.Min(min, i);
                max = Math.Max(max, i);
            }
            return new float[] { min, max };
        }

        public static int[] Range(this IEnumerable<int> arr)
        {
            int min = int.MaxValue;
            int max = int.MinValue;
            foreach (var i in arr)
            {
                min = Math.Min(min, i);
                max = Math.Max(max, i);
            }
            return new int[] { min, max };
        }

        public static short[] Range(this IEnumerable<short> arr)
        {
            short min = short.MaxValue;
            short max = short.MinValue;
            foreach (var i in arr)
            {
                min = Math.Min(min, i);
                max = Math.Max(max, i);
            }
            return new short[] { min, max };
        }

        #endregion

        #region 数组累乘

        public static int Product(this IEnumerable<int> arr)
        {
            int prod = 1;
            foreach (var i in arr) prod *= i;
            return prod;
        }

        public static uint Product(this IEnumerable<uint> arr)
        {
            uint prod = 1;
            foreach (var i in arr) prod *= i;
            return prod;
        }

        public static double Product(this IEnumerable<double> arr)
        {
            double prod = 1;
            foreach (var i in arr) prod *= i;
            return prod;
        }

        public static float Product(this IEnumerable<float> arr)
        {
            float prod = 1;
            foreach (var i in arr) prod *= i;
            return prod;
        }

        #endregion

        #region 数组切片

        /// <summary>
        /// 数组切片，目前只支持三维数组
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sourceDims">源数据的维度{width, height, depth}</param>
        /// <param name="sliceRange">切片范围{xmin,xmax,ymin,ymax,zmin,zmax}</param>
        /// <returns></returns>
        public static Array Slicing(this Array source, int[] sourceDims, int[] sliceRange)
        {
            if (sourceDims.Length != 3) throw new ArgumentException("sourceDims length mast be 3");
            if (sliceRange.Length != 6) throw new ArgumentException("sliceRange length mast be 6");
            int zstep = sliceRange[5] - sliceRange[4];
            int ystep = sliceRange[3] - sliceRange[2];
            int xstep = sliceRange[1] - sliceRange[0];
            int size = zstep * ystep * xstep;
            int inputPageSize = sourceDims[1] * sourceDims[0];
            int outputPageSize = ystep * xstep;
            Array output = Array.CreateInstance(source.GetValue(0).GetType(), size);
            for (int iz = sliceRange[4]; iz < sliceRange[5]; iz++)
            {
                int oz = iz - sliceRange[4];
                int iz0 = iz * inputPageSize;
                int oz0 = oz * outputPageSize;
                for (int iy = sliceRange[2]; iy < sliceRange[3]; iy++)
                {
                    int oy = iy - sliceRange[2];
                    int iIndex = iz0 + iy * sourceDims[0] + sliceRange[0];
                    int oIndex = oz0 + oy * xstep;
                    Array.Copy(source, iIndex, output, oIndex, xstep);
                }
            }
            return output;
        }

        /// <summary>
        /// 数组重新设置大小，目前只支持三维数组
        /// </summary>
        /// <param name="input"></param>
        /// <param name="dims"></param>
        /// <param name="origin"></param>
        /// <param name="newDims"></param>
        /// <returns></returns>
        public static Array Resize(this Array input, int[] dims, int[] origin, int[] newDims)
        {
            if (dims.Length != 3) throw new ArgumentException("dims length mast be 3");
            if (origin.Length != 3) throw new ArgumentException("origin length mast be 3");
            if (newDims.Length != 3) throw new ArgumentException("newDims length mast be 6");
            int size = newDims[2] * newDims[1] * newDims[0];
            Array output = Array.CreateInstance(input.GetValue(0).GetType(), size);
            for (int iz = 0; iz < dims[2]; iz++)
            {
                int oz = iz + origin[2];
                int iz0 = iz * dims[1] * dims[0];
                int oz0 = oz * newDims[1] * newDims[0];
                for (int iy = 0; iy < dims[1]; iy++)
                {
                    int oy = iy + origin[1];
                    int iIndex = iz0 + iy * dims[0];
                    int oIndex = oz0 + oy * newDims[0] + origin[0];
                    Array.Copy(input, iIndex, output, oIndex, dims[0]);
                }
            }
            return output;
        }

        #endregion
    }
}
