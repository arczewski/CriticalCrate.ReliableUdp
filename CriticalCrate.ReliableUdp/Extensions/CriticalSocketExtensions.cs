using CriticalCrate.ReliableUdp.Channels;

namespace CriticalCrate.ReliableUdp.Extensions;

public static class CriticalSocketExtensions
{
   public static Client CreateClient(TimeSpan pingInterval, TimeSpan connectionTimeout)
   {
      var packetFactory = new PacketFactory();
      var socket = new UdpSocket(packetFactory);
      return new Client(
         socket, 
         new UnreliableChannel(socket, packetFactory),
         new ReliableChannel(socket, packetFactory),
         new PingChannel(socket, packetFactory, pingInterval),
         new ClientConnectionManager(socket, packetFactory, connectionTimeout));
   }

   public static Server CreateServer(TimeSpan pingInterval, TimeSpan connectionTimeout, int maxConnections)
   {
      var packetFactory = new PacketFactory();
      var socket = new UdpSocket(packetFactory);
      return new Server(
         socket, 
         new UnreliableChannel(socket, packetFactory),
         new ReliableChannel(socket, packetFactory),
         new PingChannel(socket, packetFactory, pingInterval),
         new ServerConnectionManager(connectionTimeout, maxConnections, socket, packetFactory));
   }
}