using System.Net;
using CriticalCrate.ReliableUdp.Channels;

namespace CriticalCrate.ReliableUdp.Extensions;

internal static class PacketExtensions
{
    public static Packet CreateUnreliable(this IPacketFactory packetFactory, in Packet packet, ushort packetId)
    {
        var newPacket = packetFactory.CreatePacket(packet.EndPoint, packet.Position - packet.Offset + UnreliableChannel.HeaderSize);
        var buffer = newPacket.Buffer;
        buffer[Constants.FlagPosition] = (byte) PacketType.Unreliable;
        buffer[Constants.VersionPosition] = Constants.Version;
        BitConverter.TryWriteBytes(buffer[Constants.PacketIdPosition..], packetId);
        packet.Buffer.CopyTo(buffer[Constants.UnreliablePacketDataPosition..]);
        return newPacket;
    }
    
    public static Packet CreatePing(this IPacketFactory packetFactory, EndPoint endPoint, ushort packetId)
    {
        var newPacket = packetFactory.CreatePacket(endPoint, PingChannel.HeaderSize);
        var buffer = newPacket.Buffer;
        buffer[Constants.FlagPosition] = (byte)PacketType.Ping;
        buffer[Constants.VersionPosition] = Constants.Version;
        BitConverter.TryWriteBytes(buffer[Constants.PacketIdPosition..], packetId);
        return newPacket;
    }

    public static Packet CreatePong(this IPacketFactory packetFactory, EndPoint endPoint, ushort packetId)
    {
        var newPacket = packetFactory.CreatePacket(endPoint, PingChannel.HeaderSize);
        var buffer = newPacket.Buffer;
        buffer[Constants.FlagPosition] = (byte)PacketType.PingAck;
        buffer[Constants.VersionPosition] = Constants.Version;
        BitConverter.TryWriteBytes(buffer[Constants.PacketIdPosition..], packetId);
        return newPacket;
    }

    public static Packet CreateReliableAck(this IPacketFactory packetFactory, Packet packet, ushort ack)
    {
        var newPacket = packetFactory.CreatePacket(packet.EndPoint, Constants.HeaderSize);
        packet.Buffer[..Constants.HeaderSize].CopyTo(newPacket.Buffer);
        newPacket.Buffer[Constants.FlagPosition] = (byte)(PacketType.Reliable | PacketType.Ack);
        BitConverter.TryWriteBytes(newPacket.Buffer[Constants.AckPosition..], ack);
        return newPacket;
    }
}