using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets.Kcp;
using System.Buffers;

namespace System.Net.Sockets.TKcp
{
    class TKcpHandle : IKcpCallback
    {
        public Action<Memory<byte>> Out;
        /// <summary>
        /// output
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="avalidLength"></param>
        public void Output(IMemoryOwner<byte> buffer, int avalidLength) {
            using (buffer) {
                if (Out!=null) {
                    Out(buffer.Memory.Slice(0, avalidLength));
                }
                
            }
        }
    }
}

