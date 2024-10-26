# ReliableUdp
## Description
Reliable udp library. Trying to make it as simple as possible and allocation free to work for game development / realtime applications. It is designed to be the bare bone minimum for game development. Reliable channel is designed for standard RPC communication and not optimized for large data/file transfers.

## Limitations
* No MTU discovery. MTU is hardcoded to 508 bytes - which appears to be overall safe value for any popular network/communication type.
* There is no unreliable packet spliting logic. Trying to send more than 508 unreliably will throw exception. You need to split data on higher level if you want to send bigger payload unreliably.
* Reliable channel can send maximum of 33291780 bytes per message ~ 31MB but I would not advice using this as a channel for sending large amounts of data. Due to hardcoded 508 mtu this would result in 65535 reliable packets.
* You need to consume received packet as soon as possible. After your receive callback returns packet is immediately returned to packet pool and treated as disposed.

### Example
https://github.com/arczewski/CriticalCrate.ReliableUdp/blob/master/CriticalCrate.ReliableUdp.Example/Program.cs
```
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
```
