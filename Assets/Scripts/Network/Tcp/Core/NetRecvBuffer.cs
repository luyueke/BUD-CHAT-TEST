// NetRecvBuffer.cs
// Create by xiaojl Mar/15/2023
// 消息数据缓冲区

using System;
using System.IO;

namespace Network.Tcp.Core
{
    internal class NetRecvBuffer
    {
        private byte[] _buffer;
        private int _length;
        private int _capacity;

        public byte[] UnCompelete
        {
            get
            {
                // 申请新的字节数组，并拷贝缓冲区数据
                var buffer = new byte[_length];
                Buffer.BlockCopy(_buffer, 0, buffer, 0, _length);

                // 返回缓冲区数据的拷贝
                return buffer;
            }
        }

        public void WriteBytes(byte[] source, int offset, int count)
        {
            if (_buffer == null)
            {
                _buffer = new byte[count];
                _length = 0;
                _capacity = count;
            }

            // 确保缓冲区容量
            EnsureCapacity(_length + count);

            // 写数据到缓冲区
            Buffer.BlockCopy(source, offset, _buffer, _length, count);

            // 更新缓冲区数据长度
            _length += count;
        }

        public MsgData TryDecode()
        {
            if (_length < MsgHeader.HeaderLength)
                return null;

            using (MemoryStream ms = new MemoryStream(_buffer))
            {
                var br = new BinaryReader(ms);

                // 读取包头数据
                var head = br.ReadBytes(MsgHeader.HeaderLength);

                // 读取消息编号
                var cmd = BitConverter.ToUInt16(head, 0);

                // 读取消息长度
                var length = BitConverter.ToInt32(head, 2);

                // 如果缓冲区中存在一条完整消息，则读取
                if (_length - MsgHeader.HeaderLength >= length)
                {
                    // 读取消息内容
                    byte[] body = br.ReadBytes(length);

                    // 封装消息数据
                    MsgData data = new MsgData(cmd, body);

                    // 移除已读数据
                    Remove(MsgHeader.HeaderLength + length);

                    // 返回消息数据
                    return data;
                }
                else
                {
                    return null;
                }
            }
        }

        private void Remove(int length)
        {
            if (_length <= length)
            {
                _length = 0;
                return;
            }

            // 申请新的字节数组，并拷贝裁剪后的缓冲区数据
            var buffer = new byte[_buffer.Length];
            Buffer.BlockCopy(_buffer, length, buffer, 0, _length - length);

            // 更新数据接收缓冲区及其长度
            _length -= length;
            _buffer = buffer;
        }

        private void EnsureCapacity(int count)
        {
            // 如果缓冲区容量足够，直接返回
            if (count <= _capacity)
                return;

            // 如果扩大2倍够用，则扩大2倍
            if (count < 2 * _capacity)
                count = 2 * _capacity;

            // 申请新的字节数组，并拷贝缓冲区数据
            var buffer = new byte[count];
            Buffer.BlockCopy(_buffer, 0, buffer, 0, _length);

            // 更新数据接收缓冲区及其容量
            _capacity = count;
            _buffer = buffer;
        }
    }
}