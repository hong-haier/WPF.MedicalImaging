using itk.simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using WPF.MedicalImaging.Extensions;

namespace WPF.MedicalImaging
{

    public static class sitkConverter
    {
        private static TypeCode[] NumberTypeCodes = new TypeCode[]
        {
            TypeCode.Double, TypeCode.Single, TypeCode.Decimal, TypeCode.Char, TypeCode.SByte, TypeCode.Byte,
            TypeCode.Int16, TypeCode.Int32, TypeCode.Int64, TypeCode.UInt16, TypeCode.UInt32, TypeCode.UInt64,
        };

        public static Image ToItkArray(this Array data, int[] dims, double[] spacing)
        {
            if (data == null || data.Length == 0) return null;
            if (dims == null || dims.Length == 0) throw new ArgumentNullException("dims cannot be empty.");
            using (var itkImageCreator = new ImportImageFilter())
            {
                itkImageCreator.SetSize(new VectorUInt32(Array.ConvertAll(dims, x => (uint)x)));
                if (spacing != null)
                {
                    itkImageCreator.SetSpacing(new VectorDouble(spacing));
                }
                switch (MappingToItkDataType(data.GetValue(0).GetType()))
                {
                    case sitkType.sitkInt8PixelIDValueEnum: itkImageCreator.SetBufferAsInt8(data.GetIntPtr()); break;
                    case sitkType.sitkUInt8PixelIDValueEnum: itkImageCreator.SetBufferAsUInt8(data.GetIntPtr()); break;
                    case sitkType.sitkInt16PixelIDValueEnum: itkImageCreator.SetBufferAsInt16(data.GetIntPtr()); break;
                    case sitkType.sitkUInt16PixelIDValueEnum: itkImageCreator.SetBufferAsUInt16(data.GetIntPtr()); break;
                    case sitkType.sitkInt32PixelIDValueEnum: itkImageCreator.SetBufferAsInt32(data.GetIntPtr()); break;
                    case sitkType.sitkUInt32PixelIDValueEnum: itkImageCreator.SetBufferAsUInt32(data.GetIntPtr()); break;
                    case sitkType.sitkInt64PixelIDValueEnum: itkImageCreator.SetBufferAsInt64(data.GetIntPtr()); break;
                    case sitkType.sitkUInt64PixelIDValueEnum: itkImageCreator.SetBufferAsUInt64(data.GetIntPtr()); break;
                    case sitkType.sitkFloat32PixelIDValueEnum: itkImageCreator.SetBufferAsFloat(data.GetIntPtr()); break;
                    case sitkType.sitkFloat64PixelIDValueEnum: itkImageCreator.SetBufferAsDouble(data.GetIntPtr()); break;
                    default: throw new NotSupportedException($"{data.GetValue(0).GetType()} is not supported.");
                }
                return itkImageCreator.Execute();
            }
        }

        public static Array ToCSharpArray(this Image image)
        {
            int size = (int)image.GetSize().Product();
            switch (image.GetPixelIDValue())
            {
                case sitkType.sitkInt8PixelIDValueEnum:
                    byte[] array0 = new byte[size];
                    Marshal.Copy(image.GetBufferAsInt8(), array0, 0, size);
                    return array0;
                case sitkType.sitkUInt8PixelIDValueEnum:
                    byte[] array1 = new byte[size];
                    Marshal.Copy(image.GetBufferAsUInt8(), array1, 0, size);
                    return array1;
                case sitkType.sitkInt16PixelIDValueEnum:
                    short[] array2 = new short[size];
                    Marshal.Copy(image.GetBufferAsInt16(), array2, 0, size);
                    return array2;
                case sitkType.sitkUInt16PixelIDValueEnum:
                    short[] array3 = new short[size];
                    Marshal.Copy(image.GetBufferAsUInt16(), array3, 0, size);
                    return array3;
                case sitkType.sitkInt32PixelIDValueEnum:
                    int[] array4 = new int[size];
                    Marshal.Copy(image.GetBufferAsInt32(), array4, 0, size);
                    return array4;
                case sitkType.sitkUInt32PixelIDValueEnum:
                    int[] array5 = new int[size];
                    Marshal.Copy(image.GetBufferAsUInt32(), array5, 0, size);
                    return array5;
                case sitkType.sitkInt64PixelIDValueEnum:
                    long[] array6 = new long[size];
                    Marshal.Copy(image.GetBufferAsInt64(), array6, 0, size);
                    return array6;
                case sitkType.sitkUInt64PixelIDValueEnum:
                    long[] array7 = new long[size];
                    Marshal.Copy(image.GetBufferAsUInt64(), array7, 0, size);
                    return array7;
                case sitkType.sitkFloat32PixelIDValueEnum:
                    float[] array8 = new float[size];
                    Marshal.Copy(image.GetBufferAsFloat(), array8, 0, size);
                    return array8;
                case sitkType.sitkFloat64PixelIDValueEnum:
                    double[] array9 = new double[size];
                    Marshal.Copy(image.GetBufferAsDouble(), array9, 0, size);
                    return array9;
                default: throw new NotSupportedException($"{image.GetPixelIDTypeAsString()} is not supported.");
            }
        }

        public static VectorUInt8 ToItkVectorUInt8<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorUInt8.");

            var vector = new VectorUInt8();
            foreach (dynamic item in array)
            {
                vector.Add((byte)item);
            }
            return vector;
        }

        public static VectorUInt16 ToItkVectorUInt16<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorUInt16.");

            var vector = new VectorUInt16();
            foreach (dynamic item in array)
            {
                vector.Add((ushort)item);
            }
            return vector;
        }

        public static VectorUInt32 ToItkVectorUInt32<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorUInt32.");

            VectorUInt32 vector = new VectorUInt32();
            foreach (dynamic item in array)
            {
                vector.Add((uint)item);
            }
            return vector;
        }

        public static VectorUInt64 ToItkVectorUInt64<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorUInt64.");

            var vector = new VectorUInt64();
            foreach (dynamic item in array)
            {
                vector.Add((ulong)item);
            }
            return vector;
        }

        public static VectorInt8 ToItkVectorInt8<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorInt8.");

            var vector = new VectorInt8();
            foreach (dynamic item in array)
            {
                vector.Add((sbyte)item);
            }
            return vector;
        }

        public static VectorInt16 ToItkVectorInt16<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorInt16.");

            var vector = new VectorInt16();
            foreach (dynamic item in array)
            {
                vector.Add((short)item);
            }
            return vector;
        }

        public static VectorInt32 ToItkVectorInt32<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorInt32.");

            var vector = new VectorInt32();
            foreach (dynamic item in array)
            {
                vector.Add((int)item);
            }
            return vector;
        }

        public static VectorInt64 ToItkVectorInt64<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorInt64.");

            var vector = new VectorInt64();
            foreach (dynamic item in array)
            {
                vector.Add((long)item);
            }
            return vector;
        }

        public static VectorFloat ToItkVectorFloat<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorFloat.");

            var vector = new VectorFloat();
            foreach (dynamic item in array)
            {
                vector.Add((float)item);
            }
            return vector;
        }

        public static VectorDouble ToItkVectorDouble<T>(this IEnumerable<T> array)
            where T : struct
        {
            if (array == null) return null;
            var type = Type.GetTypeCode(array.FirstOrDefault().GetType());
            if (!NumberTypeCodes.Contains(type))
                throw new NotSupportedException($"{type} is cannot be converted to VectorDouble.");

            var vector = new VectorDouble();
            foreach (dynamic item in array)
            {
                vector.Add((double)item);
            }
            return vector;
        }

        private static int MappingToItkDataType(Type type)
        {
            if (type == typeof(char)) return sitkType.sitkInt8PixelIDValueEnum;
            else if (type == typeof(byte)) return sitkType.sitkInt8PixelIDValueEnum;
            else if (type == typeof(sbyte)) return sitkType.sitkInt8PixelIDValueEnum;
            else if (type == typeof(short)) return sitkType.sitkInt16PixelIDValueEnum;
            else if (type == typeof(ushort)) return sitkType.sitkUInt16PixelIDValueEnum;
            else if (type == typeof(int)) return sitkType.sitkInt32PixelIDValueEnum;
            else if (type == typeof(uint)) return sitkType.sitkUInt32PixelIDValueEnum;
            else if (type == typeof(long)) return sitkType.sitkInt64PixelIDValueEnum;
            else if (type == typeof(ulong)) return sitkType.sitkUInt64PixelIDValueEnum;
            else if (type == typeof(float)) return sitkType.sitkFloat32PixelIDValueEnum;
            else if (type == typeof(double)) return sitkType.sitkFloat64PixelIDValueEnum;
            return sitkType.sitkUnknownPixelIDValueEnum;
        }

    }
}
