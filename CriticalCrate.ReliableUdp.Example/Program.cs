using System.Net;
using CriticalCrate.ReliableUdp;
using CriticalCrate.ReliableUdp.Extensions;

var server = SocketFactory.CreateServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), 1);
server.Listen(new IPEndPoint(IPAddress.Any, 5000));
server.OnPacketReceived += packet =>
{
    Console.WriteLine(System.Text.Encoding.UTF8.GetString(packet.Buffer));
    var pongMessage = "Hello World from server"u8.ToArray();
    server.Send(server.PacketFactory.CreatePacket(packet.EndPoint, pongMessage, 0, pongMessage.Length),
        SendMode.Reliable);
};

var client = SocketFactory.CreateClient(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
client.Connect(new IPEndPoint(IPAddress.Loopback, 5000));
client.OnPacketReceived += packet => { Console.WriteLine(System.Text.Encoding.UTF8.GetString(packet.Buffer)); };
var pingMessage = "Hello World from client"u8.ToArray();

client.OnConnected += () =>
{
    client.Send(client.PacketFactory.CreatePacket(client.ServerEndpoint, pingMessage, 0, pingMessage.Length),
        SendMode.Reliable);
};

while (!Console.KeyAvailable)
{
    client.Pool(); // pool messages from client socket
    server.Pool(); // pool messages from server socket
}

client.Dispose();
server.Dispose();