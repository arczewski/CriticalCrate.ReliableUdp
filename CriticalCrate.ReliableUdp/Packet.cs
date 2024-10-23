using System.Buffers;
using System.Net;

namespace CriticalCrate.ReliableUdp
{
    public struct Packet
    {
        public EndPoint EndPoint;
        internal SocketAddress SocketAddress = null!;
        public Span<byte> Buffer => _owner.Memory.Span[Offset..Position];
        private readonly IMemoryOwner<byte> _owner;
        public int Position { get; set; }
        public int Offset { get; set; }
        
        internal Packet(EndPoint endPoint, IMemoryOwner<byte> owner, int offset, int position)
        {
            EndPoint = endPoint;
            Offset = offset;
            Position = position;
            _owner = owner;
        }

        internal void SetSocketAddress(SocketAddress socketAddress)
        {
            SocketAddress = socketAddress;
        }

        internal void ReturnBorrowedMemory()
        {
            _owner.Dispose();
        }
    }
}