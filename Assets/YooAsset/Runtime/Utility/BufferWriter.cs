using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace YooAsset
{
    /// <summary>
    /// 数据存储以小端字节序为标准
    /// </summary>
    internal class BufferWriter
    {
        private readonly byte[] _buffer;
        private int _index = 0;

        /// <summary>
        /// 创建指定容量的缓冲区写入器
        /// </summary>
        public BufferWriter(int capacity)
        {
            _buffer = new byte[capacity];
        }

        /// <summary>
        /// 缓冲区容量
        /// </summary>
        public int Capacity
        {
            get { return _buffer.Length; }
        }

        /// <summary>
        /// 清空缓冲区
        /// </summary>
        public void Clear()
        {
            _index = 0;
        }

        /// <summary>
        /// 获取有效数据
        /// </summary>
        public byte[] GetBytes()
        {
            byte[] newArray = new byte[_index];
            Buffer.BlockCopy(_buffer, 0, newArray, 0, _index);
            return newArray;
        }

        /// <summary>
        /// 将有效数据写入文件流
        /// </summary>
        public void WriteToStream(FileStream fileStream)
        {
            fileStream.Write(_buffer, 0, _index);
        }

        /// <summary>
        /// 写入字节数组
        /// </summary>
        public void WriteBytes(byte[] data)
        {
            int count = data.Length;
            CheckWriterIndex(count);
            Buffer.BlockCopy(data, 0, _buffer, _index, count);
            _index += count;
        }

        /// <summary>
        /// 写入单个字节
        /// </summary>
        public void WriteByte(byte value)
        {
            CheckWriterIndex(1);
            _buffer[_index++] = value;
        }

        /// <summary>
        /// 写入布尔值
        /// </summary>
        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }

        /// <summary>
        /// 写入16位有符号整数
        /// </summary>
        public void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }

        /// <summary>
        /// 写入16位无符号整数
        /// </summary>
        public void WriteUInt16(ushort value)
        {
            CheckWriterIndex(2);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
        }

        /// <summary>
        /// 写入32位有符号整数
        /// </summary>
        public void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }

        /// <summary>
        /// 写入32位无符号整数
        /// </summary>
        public void WriteUInt32(uint value)
        {
            CheckWriterIndex(4);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
            _buffer[_index++] = (byte)(value >> 16);
            _buffer[_index++] = (byte)(value >> 24);
        }

        /// <summary>
        /// 写入64位有符号整数
        /// </summary>
        public void WriteInt64(long value)
        {
            WriteUInt64((ulong)value);
        }

        /// <summary>
        /// 写入64位无符号整数
        /// </summary>
        public void WriteUInt64(ulong value)
        {
            CheckWriterIndex(8);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
            _buffer[_index++] = (byte)(value >> 16);
            _buffer[_index++] = (byte)(value >> 24);
            _buffer[_index++] = (byte)(value >> 32);
            _buffer[_index++] = (byte)(value >> 40);
            _buffer[_index++] = (byte)(value >> 48);
            _buffer[_index++] = (byte)(value >> 56);
        }

        /// <summary>
        /// 写入UTF8编码的字符串
        /// </summary>
        public void WriteUTF8(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                WriteUInt16(0);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(value);
                int count = bytes.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write string length cannot be greater than {ushort.MaxValue}");

                WriteUInt16(Convert.ToUInt16(count));
                WriteBytes(bytes);
            }
        }

        /// <summary>
        /// 写入32位有符号整数数组
        /// </summary>
        public void WriteInt32Array(int[] values)
        {
            if (values == null)
            {
                WriteUInt16(0);
            }
            else
            {
                int count = values.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue}");

                WriteUInt16(Convert.ToUInt16(count));
                for (int i = 0; i < count; i++)
                {
                    WriteInt32(values[i]);
                }
            }
        }

        /// <summary>
        /// 写入64位有符号整数数组
        /// </summary>
        public void WriteInt64Array(long[] values)
        {
            if (values == null)
            {
                WriteUInt16(0);
            }
            else
            {
                int count = values.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue}");

                WriteUInt16(Convert.ToUInt16(count));
                for (int i = 0; i < count; i++)
                {
                    WriteInt64(values[i]);
                }
            }
        }

        /// <summary>
        /// 写入UTF8编码的字符串数组
        /// </summary>
        public void WriteUTF8Array(string[] values)
        {
            if (values == null)
            {
                WriteUInt16(0);
            }
            else
            {
                int count = values.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue}");

                WriteUInt16(Convert.ToUInt16(count));
                for (int i = 0; i < count; i++)
                {
                    WriteUTF8(values[i]);
                }
            }
        }

        [Conditional("DEBUG")]
        private void CheckWriterIndex(int length)
        {
            if (_index + length > Capacity)
            {
                throw new IndexOutOfRangeException();
            }
        }
    }
}