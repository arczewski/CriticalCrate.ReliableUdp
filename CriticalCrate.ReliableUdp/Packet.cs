using System.Buffers;
using System.Net;

namespace CriticalCrate.ReliableUdp
{
    public struct Packet
    {
        public EndPoint EndPoint;
        public Span<byte> Buffer => _owner.Memory.Span[Offset..Position];
        private readonly IMemoryOwner<byte> _owner;
        internal int Position { get; init; }
        internal int Offset { get; init; }
        
        internal Packet(EndPoint endPoint, IMemoryOwner<byte> owner, int offset, int position)
        {
            EndPoint = endPoint;
            Offset = offset;
            Position = position;
            _owner = owner;
        }

        internal void ReturnBorrowedMemory()
        {
            _owner.Dispose();
        }
    }
}