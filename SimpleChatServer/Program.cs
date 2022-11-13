using SimpleChatServer;


var server = new ChatServer(31200);

server.Start();
Console.WriteLine($"채팅 서버가 {server.Port} 포트로 시작됨.");

Console.ReadLine();

server.Stop();
Console.WriteLine($"채팅 서버 중지됨.");
