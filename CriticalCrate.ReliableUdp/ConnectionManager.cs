using System.Net;
using CriticalCrate.ReliableUdp.Channels;
using CriticalCrate.ReliableUdp.Exceptions;

namespace CriticalCrate.ReliableUdp;

public interface IConnectionManager : IPacketHandler
{
    void CheckConnectionTimeout(DateTime now);
}

public interface IClientConnectionManager : IConnectionManager
{
    bool Connected { get; }
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action OnConnectionTimeout;
    void Connect(EndPoint endPoint);
}

internal sealed class ClientConnectionManager(
    ISocket socket,
    IPacketFactory packetFactory,
    TimeSpan connectionTimeout) : IClientConnectionManager 
{
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action? OnConnectionTimeout;
    public bool Connected { get; private set; }
    private EndPoint? _serverEndpoint;
    private DateTime _lastReceivedPacketTime;

    public void CheckConnectionTimeout(DateTime now)
    {
        if (_lastReceivedPacketTime.Add(connectionTimeout) > now)
            return;
        OnConnectionTimeout?.Invoke();
    }

    public void Connect(EndPoint endPoint)
    {
        _serverEndpoint = endPoint;
        var connectionPacket = packetFactory.CreatePacket(_serverEndpoint, UnreliableChannel.HeaderSize);
        connectionPacket.Buffer[Constants.FlagPosition] = (byte)PacketType.Connect;
        connectionPacket.Buffer[Constants.VersionPosition] = Constants.Version;
        socket.Send(connectionPacket);
    }

    public void Disconnect()
    {
        if (!Connected || _serverEndpoint == null) return;
        var disconnectPacket = packetFactory.CreatePacket(_serverEndpoint, UnreliableChannel.HeaderSize);
        disconnectPacket.Buffer[Constants.FlagPosition] = (byte)PacketType.Disconnect;
        disconnectPacket.Buffer[Constants.VersionPosition] = Constants.Version;
        socket.Send(disconnectPacket);
        Connected = false;
        OnDisconnected?.Invoke();
    }

    public void HandlePacket(in Packet receivedPacket, in PacketType packetType, in ushort packetId)
    {
        _lastReceivedPacketTime = DateTime.Now;
        if (packetType == PacketType.ServerFull)
            throw new ServerIsFullException();

        if (packetType.HasFlag(PacketType.Disconnect))
        {
            if (Connected)
                OnDisconnected?.Invoke();
            Connected = false;
            return;
        }

        if (!packetType.HasFlag(PacketType.Connect)) return;
        if (Connected) return;
        Connected = true;
        OnConnected?.Invoke();
    }
}

public interface IServerConnectionManager : IConnectionManager
{
    event Action<EndPoint> OnConnected;
    event Action<EndPoint> OnDisconnected;
    bool IsConnected(EndPoint endPoint);
    IReadOnlyCollection<EndPoint> ConnectedClients { get; }
}

internal sealed class ServerConnectionManager(
    TimeSpan connectionTimeout,
    int maxConnection,
    ISocket socket,
    IPacketFactory packetFactory)
    : IServerConnectionManager
{
    public event Action<EndPoint>? OnConnected;
    public event Action<EndPoint>? OnDisconnected;
    public IReadOnlyCollection<EndPoint> ConnectedClients => _lastReceivedPacket.Keys;

    private readonly Dictionary<EndPoint, DateTime> _lastReceivedPacket = [];
    private readonly List<EndPoint> _endPointsToDisconnect = [];

    public void CheckConnectionTimeout(DateTime now)
    {
        _endPointsToDisconnect.Clear();
        foreach (var keyValue in _lastReceivedPacket.Where(keyValue => keyValue.Value.Add(connectionTimeout) < now))
        {
            _endPointsToDisconnect.Add(keyValue.Key);
        }

        foreach (var endpoint in _endPointsToDisconnect)
            SendDisconnect(endpoint);
    }

    public bool IsConnected(EndPoint endPoint)
    {
        return _lastReceivedPacket.ContainsKey(endPoint);
    }

    public void HandlePacket(in Packet packet, in PacketType packetType, in ushort packetId)
    {
        if (packetType.HasFlag(PacketType.Connect))
        {
            if (_lastReceivedPacket.Count >= maxConnection)
            {
                SendServerFull(packet.EndPoint);
            }

            if (_lastReceivedPacket.ContainsKey(packet.EndPoint))
            {
                SendConnectionApproval(packet.EndPoint);
                return;
            }

            _lastReceivedPacket.Add(packet.EndPoint, DateTime.Now);
            SendConnectionApproval(packet.EndPoint);
            OnConnected?.Invoke(packet.EndPoint);
            return;
        }

        if (packetType.HasFlag(PacketType.Disconnect))
        {
            if (!_lastReceivedPacket.Remove(packet.EndPoint))
                return;
            OnDisconnected?.Invoke(packet.EndPoint);
            return;
        }

        _lastReceivedPacket[packet.EndPoint] = DateTime.Now;
    }

    private void SendServerFull(EndPoint endPoint)
    {
        var packet = packetFactory.CreatePacket(endPoint, UnreliableChannel.HeaderSize);
        packet.Buffer[Constants.FlagPosition] = (byte)PacketType.ServerFull;
        packet.Buffer[Constants.VersionPosition] = Constants.Version;
        socket.Send(packet);
    }

    private void SendConnectionApproval(EndPoint endPoint)
    {
        var packet = packetFactory.CreatePacket(endPoint, UnreliableChannel.HeaderSize);
        packet.Buffer[Constants.FlagPosition] = (byte)PacketType.Connect;
        packet.Buffer[Constants.VersionPosition] = Constants.Version;
        socket.Send(packet);
    }

    private void SendDisconnect(EndPoint endPoint)
    {
        var packet = packetFactory.CreatePacket(endPoint, UnreliableChannel.HeaderSize);
        packet.Buffer[Constants.FlagPosition] = (byte)PacketType.Disconnect;
        packet.Buffer[Constants.VersionPosition] = Constants.Version;
        socket.Send(packet);
        _lastReceivedPacket.Remove(endPoint);
    }
}