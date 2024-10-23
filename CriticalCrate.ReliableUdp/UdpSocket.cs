using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace CriticalCrate.ReliableUdp;

public sealed class UdpSocket(IPacketFactory packetFactory) : ISocket
{
    public event OnPacketReceived? OnPacketReceived;

    private readonly Socket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private Packet _receivePacket = packetFactory.CreatePacket(new IPEndPoint(0, 0), ISocket.Mtu);

    public void Listen(EndPoint endPoint)
    {
        _receivePacket.SetSocketAddress(_receivePacket.EndPoint.Serialize());
        _listenSocket.Bind(endPoint);
        _listenSocket.Blocking = false;
        _listenSocket.ReceiveBufferSize = 1024 * 1024 * 32;
        _listenSocket.SendBufferSize = 1024 * 1024 * 32;
    }

    public void Send(Packet packet)
    {
        Debug.Assert(packet.SocketAddress != null);
        if (!_listenSocket.Poll(0, SelectMode.SelectWrite))
            return;
        _listenSocket.SendTo(packet.Buffer, SocketFlags.None, socketAddress: packet.SocketAddress);
    }

    public bool Pool()
    {
        if (_listenSocket.Available == 0)
            return false;
        _receivePacket = _receivePacket with { Position = ISocket.Mtu, Offset = 0 };
        var byteCount = _listenSocket.ReceiveFrom(_receivePacket.Buffer, SocketFlags.None, ref _receivePacket.EndPoint);
        if (byteCount == 0)
            return false;
        _receivePacket = _receivePacket with { Position = byteCount };
        OnPacketReceived?.Invoke(_receivePacket);
        return true;
    }

    public void Dispose()
    {
        _listenSocket.Dispose();
    }
}