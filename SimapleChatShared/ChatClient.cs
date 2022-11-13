using System.Net.Sockets;

using static SimapleChatShared.ChatPacket;

namespace SimapleChatShared
{
    public class ChatClient : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private bool disposedValue;


        public string? Authtoken {  get; set; }
        public string? Id { get; set; }
        public string? Nickname {  get; set; }


        public event EventHandler<ReceivedChatPacketEventArgs>? ReceivedChatPacketEvent;


        public ChatClient(TcpClient tcpClient)
        {
            this._tcpClient = tcpClient;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //if (Authtoken is not null)
                    //    Send(new ChatRequestGoodBye(Authtoken));
                    _tcpClient.Dispose();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        public void Send(IChatPacket packet)
        {
            ChatPacket.Send(packet, _tcpClient.GetStream());
        }

        public async Task<IChatPacket> ReceiveAsync()
        {
            var result = await ChatPacket.ReceiveAsync(_tcpClient.GetStream());

            if (result is ChatHelloRequest requestHello)
            {
                Id = requestHello.Id;
                Nickname = requestHello.Nickname;
            }
            else if (result is ChatHelloResponse responseHello)
                Authtoken = responseHello.Authtoken;

            ReceivedChatPacketEvent?.Invoke(this, new ReceivedChatPacketEventArgs(this, result));

            return result;
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        // ~ChatClient()
        // {
        //     // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"{Nickname}({Id}, {_tcpClient.Client.RemoteEndPoint})";
        }
    }


    public class ReceivedChatPacketEventArgs : EventArgs
    {
        public ChatClient Client { get; }
        public IChatPacket Packet { get; }

        public ReceivedChatPacketEventArgs(ChatClient client, IChatPacket packet)
        {
            Client = client;
            Packet = packet;
        }
    }
}