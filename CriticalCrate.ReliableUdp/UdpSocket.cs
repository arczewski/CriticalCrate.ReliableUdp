using System.Net;
using System.Net.Sockets;

namespace CriticalCrate.ReliableUdp;

internal sealed class UdpSocket(IPacketManager packetManager, int sendBufferSize = 1024 * 1024 * 4, int receiveBufferSize = 1024 * 1024 * 4) : ISocket
{
    public event OnPacketReceived? OnPacketReceived;
    private readonly Socket _listenSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private readonly Dictionary<EndPoint, SocketAddress> _socketAddresses = new();
    private Packet _receivePacket = packetManager.CreatePacket(new IPEndPoint(0, 0), ISocket.Mtu);
    
    public void Listen(EndPoint endPoint)
    {
        _listenSocket.Bind(endPoint);
        _listenSocket.Blocking = false;
        _listenSocket.ReceiveBufferSize = receiveBufferSize;
        _listenSocket.SendBufferSize = sendBufferSize;
    }

    public void Send(in Packet packet)
    {
        if (!_listenSocket.Poll(0, SelectMode.SelectWrite))
        {
            packetManager.ReturnPacket(packet);
            return;
        }

        if (!_socketAddresses.TryGetValue(packet.EndPoint, out var socketAddress))
        {
            socketAddress = packet.EndPoint.Serialize();
            _socketAddresses.Add(packet.EndPoint, socketAddress);
        }
        _listenSocket.SendTo(packet.Buffer, SocketFlags.None, socketAddress: socketAddress);
        packetManager.ReturnPacket(packet);
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