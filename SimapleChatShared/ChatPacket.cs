using System.Net.Http;
using System.Net.Sockets;
using System.Text;

namespace SimapleChatShared;

public static class ChatPacket
{
    public static async Task<IChatPacket> ReceiveAsync(Stream s)
    {
        try
        {
            var temp = new byte[1];
            await s.ReadExactlyAsync(temp, 0, 1);

            var command = (CommandType)temp[0];
            IChatPacket result = command switch
            {
                CommandType.REQ_HELLO => ChatHelloRequest.Receive(s),
                CommandType.RES_HELLO => ChatHelloResponse.Receive(s),
                CommandType.REQ_GOODBYE => ChatGoodbyeRequest.Receive(s),
                CommandType.RES_GOODBYE => ChatGoodbyeResponse.Receive(s),
                CommandType.REQ_MESSAGE => ChatMessageRequest.Receive(s),
                CommandType.RES_MESSAGE => ChatMessageResponse.Receive(s),
                CommandType.EVT_MESSAGE => ChatMessageEvent.Receive(s),
                CommandType.EVT_INFO => ChatInfoEvent.Receive(s),
                _ => new ChatPacketError(ChatPacketErrorKind.InvalidCommand)
            };

            return result;
        }
        // 강제 연결 종료
        catch (EndOfStreamException)
        {
            return new ChatPacketError(ChatPacketErrorKind.ForceDisconnected);
        }
        // 기타 알수 없는 이유 (패킷 비정상)
        catch (Exception)
        {
            return new ChatPacketError(ChatPacketErrorKind.InvalidPacket);
        }
    }

    public static void Send(IChatPacket packet, NetworkStream s)
    {
        s.WriteByte((byte)packet.Command);
        packet.Send(s);
        s.Flush();
    }
}

public record ChatHelloRequest(string Id, string Nickname) : IChatPacket<ChatHelloRequest>
{
    public CommandType Command => CommandType.REQ_HELLO;

    public static ChatHelloRequest Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        return new ChatHelloRequest(br.ReadString(), br.ReadString());
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write(Id);
        bw.Write(Nickname);
    }
}
public record ChatHelloResponse(string Authtoken, string Id) : IChatPacket<ChatHelloResponse>
{
    public CommandType Command => CommandType.RES_HELLO;

    public static ChatHelloResponse Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        return new ChatHelloResponse(br.ReadString(), br.ReadString());
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write(Authtoken);
        bw.Write(Id);
    }
}
public record ChatGoodbyeRequest(string Authtoken) : IChatPacket<ChatGoodbyeRequest>
{
    public CommandType Command => CommandType.REQ_GOODBYE;

    public static ChatGoodbyeRequest Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        return new ChatGoodbyeRequest(br.ReadString());
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write(Authtoken);
    }
}
public record ChatGoodbyeResponse() : IChatPacket<ChatGoodbyeResponse>
{
    public CommandType Command => CommandType.RES_GOODBYE;

    public static ChatGoodbyeResponse Receive(Stream s)
    {
        return new ChatGoodbyeResponse();
    }

    public void Send(Stream s)
    {
    }
}
public record ChatMessageRequest(string Authtoken, string Message) : IChatPacket<ChatMessageRequest>
{
    public CommandType Command => CommandType.REQ_MESSAGE;

    public static ChatMessageRequest Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        return new ChatMessageRequest(br.ReadString(), br.ReadString());
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write(Authtoken);
        bw.Write(Message);
    }
}
public record ChatMessageResponse() : IChatPacket<ChatMessageResponse>
{
    public CommandType Command => CommandType.RES_MESSAGE;

    public static ChatMessageResponse Receive(Stream s)
    {
        return new ChatMessageResponse();
    }

    public void Send(Stream s)
    {
    }
}
public record ChatMessageEvent(string Id, string Nickname, string Message) : IChatPacket<ChatMessageEvent>
{
    public CommandType Command => CommandType.EVT_MESSAGE;

    public static ChatMessageEvent Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        return new ChatMessageEvent(br.ReadString(), br.ReadString(), br.ReadString());
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write(Id);
        bw.Write(Nickname);
        bw.Write(Message);
    }
}

public record ChatPacketError(ChatPacketErrorKind ErrorKind) : IChatPacket<ChatPacketError>
{
    public CommandType Command => CommandType.ERR_PACKET;

    public static ChatPacketError Receive(Stream s)
    {
        throw new NotImplementedException();
    }

    public void Send(Stream s)
    {
        throw new NotImplementedException();
    }
}

public record ChatInfoEvent(ChatInfoKind InfoKind, IReadOnlyDictionary<string, string> InfoMap) : IChatPacket<ChatInfoEvent>
{
    public CommandType Command => CommandType.EVT_INFO;

    public static ChatInfoEvent Receive(Stream s)
    {
        using var br = new BinaryReader(s, Encoding.UTF8, true);
        var infoKind = (ChatInfoKind)br.ReadByte();
        var length = br.ReadInt32();

        var infoMap = new Dictionary<string, string>();
        for (var i = 0; i < length; i++)
        {
            var key = br.ReadString();
            var value = br.ReadString();

            infoMap[key] = value;
        }

        return new ChatInfoEvent(infoKind, infoMap);
    }

    public void Send(Stream s)
    {
        using var bw = new BinaryWriter(s, Encoding.UTF8, true);
        bw.Write((byte)InfoKind);
        bw.Write(InfoMap.Count);

        foreach (var kv in InfoMap)
        {
            bw.Write(kv.Key);
            bw.Write(kv.Value);
        }
    }
}


public enum CommandType
{
    REQ_HELLO = 0x00,
    RES_HELLO = 0x10,

    REQ_GOODBYE = 0x01,
    RES_GOODBYE = 0x11,

    REQ_MESSAGE = 0x02,
    RES_MESSAGE = 0x12,

    EVT_MESSAGE = 0x03,
    EVT_INFO = 0x04,

    ERR_PACKET = 0xFF,
}

public enum ChatPacketErrorKind
{
    InvalidCommand,
    InvalidPacket,
    ForceDisconnected
}

public enum ChatInfoKind
{
    Users
}

public interface IChatPacket
{
    CommandType Command { get; }
    void Send(Stream s);
}

public interface IChatPacket<T> : IChatPacket
{
    abstract static T Receive(Stream s);
}
