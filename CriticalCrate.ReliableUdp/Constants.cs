namespace CriticalCrate.ReliableUdp;

public static class Constants
{
    public const int HeaderSize = FlagSize + VersionSize + PacketIdSize + PacketsCountSize + SeqAckSize;
    public const int AckPosition = FlagSize + VersionSize + PacketIdSize + PacketsCountSize;
    public const int PacketsCountPosition = FlagSize + VersionSize + PacketIdSize;
    public const int PacketIdPosition = FlagSize + VersionSize;
    public const int VersionPosition = FlagSize;
    public const int FlagPosition = 0;
    public const int UnreliablePacketDataPosition = FlagSize + VersionSize + PacketIdSize;

    private const int FlagSize = sizeof(byte);
    private const int VersionSize = sizeof(byte);
    private const int PacketIdSize = sizeof(ushort);
    private const int PacketsCountSize = sizeof(ushort);
    private const int SeqAckSize = sizeof(ushort);
    public const byte Version = 1;
}