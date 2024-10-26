using System.Net;
using CriticalCrate.ReliableUdp.Channels;

namespace CriticalCrate.ReliableUdp;

public sealed class Client(
    ISocket socket,
    IUnreliableChannel unreliableChannel,
    IReliableChannel reliableChannel,
    IPingChannel pingChannel,
    IClientConnectionManager clientConnectionManager,
    IPacketFactory packetFactory)
    : CriticalSocket(socket, unreliableChannel, reliableChannel, pingChannel, clientConnectionManager, packetFactory)
{
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public IPEndPoint ServerEndpoint { get; private set; }
    public IClientConnectionManager ConnectionManager { get; } = clientConnectionManager;

    public void Connect(IPEndPoint endPoint)
    {
        ServerEndpoint = endPoint;
        var localEndpoint = new IPEndPoint(IPAddress.Any, 0); // 0 means random port
        socket.Listen(localEndpoint);
        ConnectionManager.OnConnected += HandleConnected;
        ConnectionManager.OnDisconnected += HandleDisconnected;
        ConnectionManager.Connect(endPoint);
    }

    private void HandleDisconnected()
    {
        reliableChannel.HandleDisconnection(ServerEndpoint);
        pingChannel.HandleDisconnection(ServerEndpoint);
        OnDisconnected?.Invoke();
    }

    private void HandleConnected()
    {
        reliableChannel.HandleConnection(ServerEndpoint);
        pingChannel.HandleConnection(ServerEndpoint);
        OnConnected?.Invoke();
    }
}