using System.Net;
using System.Text;
using CriticalCrate.ReliableUdp.Extensions;
using FluentAssertions;
using JetBrains.dotMemoryUnit;

namespace CriticalCrate.ReliableUdp.Tests;

public class IntegrationTests
{
    [Fact]
    public void Can_Connect()
    {
        // Arrange
        using var client = SocketFactory.CreateClient(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        using var server = SocketFactory.CreateServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), 1);

        // Act
        server.Listen(new IPEndPoint(IPAddress.Any, 4444));
        client.Connect(new IPEndPoint(IPAddress.Loopback, 4444));
        while (server.ConnectionManager.ConnectedClients.Count != 1 || !client.ConnectionManager.Connected)
        {
            server.Pool();
            client.Pool();
        }

        // Assert
        client.ConnectionManager.Connected.Should().BeTrue();
        server.ConnectionManager.ConnectedClients.Should().HaveCount(1);
    }

    [Fact]
    public void Packet_Creation_Should_Not_Allocate_Memory()
    {
        // Arrange
        var endpoint = new IPEndPoint(IPAddress.Loopback, 5000);
        var packetFactory = new PacketManager();
        var message = "Hello World"u8.ToArray();

        // Act
        var memoryBefore = GC.GetAllocatedBytesForCurrentThread();
        var packet = packetFactory.CreatePacket(endpoint, message, 0, message.Length);
        var packet2 = packetFactory.CreatePacket(endpoint, 10);
        var packet3 = packetFactory.CreatePacket(in packet);

        // Assert
        memoryBefore.Should().Be(memoryBefore);
        packet.Should().NotBeEquivalentTo(packet2);
        packet.Should().NotBeEquivalentTo(packet3);
        packet2.Should().NotBeEquivalentTo(packet3);
    }

    [Fact]
    public void Can_Send_And_Receive_Unreliable()
    {
        // Arrange
        using var client = SocketFactory.CreateClient(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        using var server = SocketFactory.CreateServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), 1);
        var packetFactory = new PacketManager();
        server.Listen(new IPEndPoint(IPAddress.Any, 4444));
        client.Connect(new IPEndPoint(IPAddress.Loopback, 4444));
        while (server.ConnectionManager.ConnectedClients.Count != 1 || !client.ConnectionManager.Connected)
        {
            server.Pool();
            client.Pool();
        }

        var messageFromClient = "Hello World from client"u8.ToArray();
        var messageFromServer = "Hello World from server"u8.ToArray();
        var messagesToSend = 100000;

        var receivedOnClient = 0;
        var receivedOnServer = 0;
        client.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromServer);
            receivedOnClient++;
        };
        server.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromClient);
            receivedOnServer++;
        };

        // Act
        for (var i = 0; i < messagesToSend; i++)
        {
            var clientPacket = packetFactory
                .CreatePacket(client.ServerEndpoint, messageFromClient, 0, messageFromClient.Length);
            var serverPacket = packetFactory
                .CreatePacket(server.ConnectionManager.ConnectedClients.Single(), messageFromServer, 0,
                    messageFromServer.Length);
            client.Send(clientPacket, SendMode.Unreliable);
            server.Send(serverPacket, SendMode.Unreliable);
            client.Pool();
            server.Pool();
        }

        // Assert

        while (receivedOnClient < messagesToSend || receivedOnServer < messagesToSend)
        {
            client.Pool();
            server.Pool();
        }

        receivedOnClient.Should().Be(messagesToSend);
        receivedOnServer.Should().Be(messagesToSend);
    }

    [Fact]
    [DotMemoryUnit(CollectAllocations = true)]
    public void Can_Send_And_Receive_Reliable_Messages()
    {
        // Arrange
        using var client = SocketFactory.CreateClient(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        using var server = SocketFactory.CreateServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), 1);
        var packetFactory = new PacketManager();
        server.Listen(new IPEndPoint(IPAddress.Any, 6666));
        client.Connect(new IPEndPoint(IPAddress.Loopback, 6666));
        while (server.ConnectionManager.ConnectedClients.Count != 1 || !client.ConnectionManager.Connected)
        {
            server.Pool();
            client.Pool();
        }

        var messageFromClient = "Hello World from client"u8.ToArray();
        var messageFromServer = "Hello World from server"u8.ToArray();
        var messagesToSend = 100000;

        // Act

        for (var i = 0; i < messagesToSend; i++)
        {
            var clientPacket = packetFactory
                .CreatePacket(client.ServerEndpoint, messageFromClient, 0, messageFromClient.Length);
            var serverPacket = packetFactory
                .CreatePacket(server.ConnectionManager.ConnectedClients.Single(), messageFromServer, 0,
                    messageFromServer.Length);
            client.Send(clientPacket, SendMode.Reliable);
            server.Send(serverPacket, SendMode.Reliable);
        }

        // Assert
        var receivedOnClient = 0;
        var receivedOnServer = 0;
        client.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromServer);
            receivedOnClient++;
        };
        server.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromClient);
            receivedOnServer++;
        };

        while (receivedOnClient < messagesToSend || receivedOnServer < messagesToSend)
        {
            client.Pool();
            server.Pool();
        }

        receivedOnClient.Should().Be(messagesToSend);
        receivedOnServer.Should().Be(messagesToSend);
    }

    [Fact]
    public void Can_Send_And_Receive_Long_Reliable_Messages()
    {
        // Arrange
        using var client = SocketFactory.CreateClient(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
        using var server = SocketFactory.CreateServer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10), 1);
        var packetFactory = new PacketManager();
        server.Listen(new IPEndPoint(IPAddress.Any, 5555));
        client.Connect(new IPEndPoint(IPAddress.Loopback, 5555));
        while (server.ConnectionManager.ConnectedClients.Count != 1 || !client.ConnectionManager.Connected)
        {
            server.Pool();
            client.Pool();
        }

        var messageFromClient =
            Encoding.UTF8.GetBytes(string.Join("", Enumerable.Repeat("Hello World from client", 10000)));
        var messageFromServer =
            Encoding.UTF8.GetBytes(string.Join("", Enumerable.Repeat("Hello World from server", 10000)));
        var messagesToSend = 100;

        // Act
        for (var i = 0; i < messagesToSend; i++)
        {
            var clientPacket = packetFactory
                .CreatePacket(client.ServerEndpoint, messageFromClient, 0, messageFromClient.Length);
            var serverPacket = packetFactory
                .CreatePacket(server.ConnectionManager.ConnectedClients.Single(), messageFromServer, 0,
                    messageFromServer.Length);
            client.Send(clientPacket, SendMode.Reliable);
            server.Send(serverPacket, SendMode.Reliable);
        }

        // Assert
        var receivedOnClient = 0;
        var receivedOnServer = 0;
        client.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromServer);
            receivedOnClient++;
        };
        server.OnPacketReceived += packet =>
        {
            packet.Buffer.ToArray().Should().BeEquivalentTo(messageFromClient);
            receivedOnServer++;
        };

        while (receivedOnClient < messagesToSend || receivedOnServer < messagesToSend)
        {
            client.Pool();
            server.Pool();
        }

        receivedOnClient.Should().Be(messagesToSend);
        receivedOnServer.Should().Be(messagesToSend);
    }
}