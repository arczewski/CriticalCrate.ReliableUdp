using System.Net;
using CriticalCrate.ReliableUdp.Channels;

namespace CriticalCrate.ReliableUdp;

public sealed class Server(
    ISocket socket,
    IUnreliableChannel unreliableChannel,
    IReliableChannel reliableChannel,
    IPingChannel pingChannel,
    IServerConnectionManager serverConnectionManager,
    IPacketFactory packetFactory)
    : CriticalSocket(socket, unreliableChannel, reliableChannel, pingChannel, serverConnectionManager, packetFactory)
{
    public event Action<EndPoint>? OnConnected;
    public event Action<EndPoint>? OnDisconnected;
    public IServerConnectionManager ConnectionManager { get; } = serverConnectionManager;

    public void Listen(IPEndPoint endPoint)
    {
        socket.Listen(endPoint);
        ConnectionManager.OnConnected += HandleConnected;
        ConnectionManager.OnDisconnected += HandleDisconnected;
    }

    private void HandleDisconnected(EndPoint endPoint)
    {
        reliableChannel.HandleDisconnection(endPoint);
        pingChannel.HandleDisconnection(endPoint);
        OnDisconnected?.Invoke(endPoint);
    }

    private void HandleConnected(EndPoint endPoint)
    {
        reliableChannel.HandleConnection(endPoint);
        pingChannel.HandleConnection(endPoint);
        OnConnected?.Invoke(endPoint);
    }
}