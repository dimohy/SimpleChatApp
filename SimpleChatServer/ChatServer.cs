using SimapleChatShared;

using System.Collections.Concurrent;
using System.Net.Sockets;


namespace SimpleChatServer;


public class ChatServer
{
    private readonly int _port;
    private TcpListener? _listener;
    private CancellationTokenSource? _serverCts;
    private Task? _listeningTask;

    private object _clientsLock = new();
    private readonly IList<ChatClient> _clients = new List<ChatClient>();


    public int Port => _port;


    public ChatServer(int port)
    {
        this._port = port;
    }

    public void Start()
    {
        _listener = TcpListener.Create(_port);
        _listener.Start();

        _serverCts = new();
        _listeningTask = Task.Run(async () =>
        {
            try
            {
                while (_serverCts.Token.IsCancellationRequested is false)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync(_serverCts.Token);

                    var client = new ChatClient(tcpClient);

                    client.ReceivedChatPacketEvent += Client_ReceivedChatPacketEvent;
                    
                    lock (_clientsLock)
                    {
                        _clients.Add(client);
                    }

                    // 비동기 수신 대기
                    _ = client.ReceiveAsync();

                    Console.WriteLine($"{client}: 연결함");
                }
            }
            catch (OperationCanceledException)
            {
                // 취소됨
            }
        }, _serverCts.Token);
    }

    public void RemoveClient(ChatClient client)
    {
        lock (_clientsLock)
        {
            _clients.Remove(client);
        }

        client.Dispose();
    }

    /// <summary>
    /// Authtoken은 사용하지 않음
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Client_ReceivedChatPacketEvent(object? sender, ReceivedChatPacketEventArgs e)
    {
        var client = e.Client;
        var packet = e.Packet;

        // 오류 처리
        if (packet is ChatPacketError chatPacketError)
        {
            Console.WriteLine($"{client}: {chatPacketError.ErrorKind}로 인해 연결 종료됨");

            lock (_clientsLock)
            {
                _clients.Remove(client);
                client.Dispose();
            }

            BroadcastUsersInfo();

            return;
        }

        // 접속
        if (packet is ChatHelloRequest)
        {
            client.Authtoken = Guid.NewGuid().ToString();
            if (string.IsNullOrWhiteSpace(client.Id) is true)
                client.Id = Guid.NewGuid().ToString();

            BroadcastUsersInfo();

            Console.WriteLine($"{client}: 접속");

            client.Send(new ChatHelloResponse(client.Authtoken, client.Id));
        }
        // 종료
        else if (packet is ChatGoodbyeRequest chatRequestGoodbye)
        {
            Console.WriteLine($"{client}: 종료");

            client.Send(new ChatGoodbyeResponse());

            RemoveClient(client);

            BroadcastUsersInfo();

            return;
        }
        // 메시지
        else if (packet is ChatMessageRequest chatRequestMessage)
        {
            Console.WriteLine($"{client}: {chatRequestMessage.Message}");

            BroadcastMessage(client, chatRequestMessage.Message);
        }

        // 비동기 수신 대기
        _ = client.ReceiveAsync();
    }

    public void BroadcastMessage(ChatClient client, string message)
    {
        IList<ChatClient> clients;
        lock (_clientsLock)
        {
            clients = _clients.ToList();
        }

        foreach (var c in clients)
            c.Send(new ChatMessageEvent(client?.Id ?? "", client?.Nickname ?? "", message));
    }

    public void BroadcastUsersInfo()
    {
        IList<ChatClient> clients;
        lock (_clientsLock)
        {
            clients = _clients.ToList();
        }

        var infoMap = new Dictionary<string, string>();
        foreach (var client in clients)
        {
            if (client.Id is null)
                continue;

            infoMap[client.Id] = client?.Nickname ?? "무명";
        }

        foreach (var c in clients)
            c.Send(new ChatInfoEvent(ChatInfoKind.Users, infoMap));
    }

    public void DisconnectAllClients()
    {
        IList<ChatClient> clients;
        lock (_clientsLock)
        {
            clients = _clients.ToList();
            _clients.Clear();
        }
        foreach (var client in clients)
        {
            client.ReceivedChatPacketEvent -= Client_ReceivedChatPacketEvent;
            client.Dispose();
        }
    }

    public void Stop()
    {
        _serverCts?.Cancel();
        _listeningTask?.Wait(); // 취소 대기

        DisconnectAllClients();

        _listener?.Stop();
    }
}
