using CriticalCrate.ReliableUdp.Channels;

namespace CriticalCrate.ReliableUdp.Extensions;

public static class SocketFactory
{
   public static Client CreateClient(TimeSpan pingInterval, TimeSpan connectionTimeout)
   {
      var packetManager = new PacketManager();
      var socket = new UdpSocket(packetManager);
      return new Client(
         socket, 
         new UnreliableChannel(socket, packetManager),
         new ReliableChannel(socket, packetManager),
         new PingChannel(socket, packetManager, pingInterval),
         new ClientConnectionManager(socket, packetManager, connectionTimeout),
         packetManager);
   }

   public static Server CreateServer(TimeSpan pingInterval, TimeSpan connectionTimeout, int maxConnections)
   {
      var packetManager = new PacketManager();
      var socket = new UdpSocket(packetManager, sendBufferSize: 1024 * 1024 * 32, receiveBufferSize: 1024 * 1024 * 32);
      return new Server(
         socket, 
         new UnreliableChannel(socket, packetManager),
         new ReliableChannel(socket, packetManager),
         new PingChannel(socket, packetManager, pingInterval),
         new ServerConnectionManager(connectionTimeout, maxConnections, socket, packetManager),
         packetManager);
   }
}