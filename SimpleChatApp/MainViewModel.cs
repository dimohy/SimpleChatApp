using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using SimapleChatShared;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SimpleChatApp
{
    [INotifyPropertyChanged]
    public partial class MainViewModel
    {
        [ObservableProperty]
        private IEnumerable<string> _users = Enumerable.Empty<string>();
        
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private string _nickname = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        private string _message = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
        [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
        private bool _connected;

        [ObservableProperty]
        private string _messages = string.Empty;


        private ChatClient? _client;


        [RelayCommand(CanExecute = nameof(CanConnect))]
        private void OnConnect()
        {
            var tcpClient = new TcpClient("localhost", 31200);
            _client = new(tcpClient);

            _ = _client.ReceiveAsync();
            _client.ReceivedChatPacketEvent += _client_ReceivedChatPacketEvent;

            _client.Send(new ChatHelloRequest(string.Empty, Nickname));

            Connected = true;
        }

        private void _client_ReceivedChatPacketEvent(object? sender, ReceivedChatPacketEventArgs e)
        {
            var packet = e.Packet;

            if (packet is ChatPacketError error)
            {
                // 에러 처리 (생략함)
                return;
            }

            if (packet is ChatInfoEvent info)
            {
                if (info.InfoKind is ChatInfoKind.Users)
                {
                    Users = info.InfoMap.Select(x => $"{x.Value}({x.Key})").ToList();
                }
            }
            else if (packet is ChatHelloResponse response)
            {
                // 처리 (생략함)
            }
            else if (packet is ChatMessageEvent message)
            {
                Messages += $"{message.Nickname}: {message.Message}\r\n";
            }
            
            _ = e.Client.ReceiveAsync();
        }

        private bool CanConnect()
        {
            return !string.IsNullOrWhiteSpace(Nickname) && Connected is false;
        }


        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private void OnSendMessage()
        {
            _client?.Send(new ChatMessageRequest(_client.Authtoken ?? "", Message));

            Message = "";
        }

        private bool CanSendMessage()
        {
            return !string.IsNullOrWhiteSpace(Message) && Connected is true;
        }
    }
}
