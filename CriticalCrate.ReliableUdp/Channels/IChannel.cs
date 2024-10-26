namespace CriticalCrate.ReliableUdp.Channels;

public interface IChannel
{
    void Send(in Packet packet);
}