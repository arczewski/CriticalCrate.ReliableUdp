using System.Net;

namespace CriticalCrate.ReliableUdp;

public readonly struct ClientAddress
{
    public EndPoint EndPoint { get; init; }
    internal SocketAddress SocketAddress { get; init; }

    public ClientAddress(EndPoint endPoint)
    {
        EndPoint = endPoint;
        SocketAddress = EndPoint.Serialize();
    }
}