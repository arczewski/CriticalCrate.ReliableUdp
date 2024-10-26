using CriticalCrate.ReliableUdp.Extensions;

namespace CriticalCrate.ReliableUdp.Channels;

public interface IUnreliableChannel : IChannel, IPacketHandler, IDisposable;

internal sealed class UnreliableChannel(ISocket socket, IPacketManager packetManager) : IUnreliableChannel
{
    public event Action<Packet>? OnPacketReceived;
    public const int HeaderSize = FlagSize + VersionSize + PacketIdSize;
    private const int FlagSize = sizeof(byte);
    private const int VersionSize = sizeof(byte);
    private const int PacketIdSize = sizeof(ushort);
    private ushort _packetId;
    public void Send(in Packet packet)
    {
        var sendPacket = packetManager.CreateUnreliable(packet, _packetId);
        socket.Send(in sendPacket);
        packetManager.ReturnPacket(packet);
        _packetId++;
    }
    
    public void HandlePacket(in Packet receivedPacket, in PacketType packetType, in ushort seq)
    {
        OnPacketReceived?.Invoke(receivedPacket);
    }

    public void Dispose()
    {
    }
}

