// See https://aka.ms/new-console-template for more information
using BallNet;
using BallSrv;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

int listen_port = 23333;
string listen_ip = "0.0.0.0";
bool isRunning = false;

BallServer srv_obj = new BallServer();
NetManager server = new NetManager(srv_obj)
{
    ReuseAddress = true,
    IPv6Mode = IPv6Mode.Disabled,
};

AppDomain appd = AppDomain.CurrentDomain;
appd.ProcessExit += (s, e) =>
{
    isRunning = false;
    server.CloseSocket(false);
    server.Stop();
};

Console.CancelKeyPress += (sender, e) =>
{
    Console.WriteLine("Press Ctrl+C");
    server.CloseSocket(false);
    server.Stop();
};

//bool result = server.Start("172.16.106.243", "localhost", 23333);
bool result = server.Start(listen_ip,"", listen_port);
if (!result)
{
    Console.WriteLine("服务器启动失败");
    return;
}
isRunning = true;
Console.WriteLine("开始监听{0}:{1}", listen_ip, listen_port);
RegisterEvent();

while (isRunning)
{
    //Console.WriteLine("Server is Running");
    RoomMgr.Instance.UpdateRoom();

    server.PollEvents();
    //if (Console.ReadKey().Key == ConsoleKey.Q)
    //{
    //    Console.WriteLine("按下Q键,服务器正在退出");
    //    break;
    //}
    Thread.Sleep(15);
}
server.CloseSocket(false);
server.Stop();
Console.WriteLine("服务器进程结束");


void RegisterEvent()
{
    EventDispatch.RegisterReceiver<NetPeer>((int)EventID.OnUserConnect, onUserConnect);
    EventDispatch.RegisterReceiver<NetPeer>((int)EventID.OnUserDisconnect, onUserDisConnect);
    EventDispatch.RegisterReceiver<SocketError>((int)EventID.OnUserError, onUserError);

    MsgDispatch.RegisterReceiver<PacketBase,NetPeer>((int)EventId.IdAuthReq, onAuthReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdRoomListReq, onRoomListReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdCreateRoomReq, onCreateRoomReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdJoinRoomReq, onJoinRoomReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdExitRoomReq, onExitRoomReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdReadyReq, onReadyReq);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdStartGameAck, onStartGameAck);
    MsgDispatch.RegisterReceiver<PacketBase, NetPeer>((int)EventId.IdUploadFrameData, onUploadFrameData);

}

void onUserConnect(NetPeer peer)
{
    int id = BallUserSessionMgr.Instance.IDGenerator;
    BallUserSession user = new BallUserSession(peer, id);
    BallUserSessionMgr.Instance.AddUser(user);
    Console.WriteLine("创建新Id {0} ip {1}",id,user.Peer.EndPoint);

}

void onUserError(SocketError err)
{
    Console.WriteLine("错误 {0}", err);
}

void onUserDisConnect(NetPeer peer)
{
    if(peer.Tag != null)
    {
        BallUserSession user = peer.Tag as BallUserSession;
        Console.WriteLine("用户Id {0} 断开连接", user.UserId);
        if(user.CurrentRoomId > 0)
        {
            Room room = RoomMgr.Instance.FindRoom(user.CurrentRoomId);
            if (room == null) return;
            room.RemoveUser(user);
            OtherExitRoomNotify notify = new OtherExitRoomNotify();
            notify.UserInfo = user.ToUserInfo();
            room.BroadcastToUser(EventId.IdOtherExitRoomNotify, 0, notify);
        }

    }
}

void onAuthReq(PacketBase pb,NetPeer peer)
{
    //Console.WriteLine("payload id = {0} payload length = {1} payload str = ", EventId.IdAuthReq.ToString(), pb.Payload.Length, BallServer.DebugDataStr(pb.Payload.ToByteArray()));

    AuthReq req = AuthReq.Parser.ParseFrom(pb.Payload);

    BallUserSession user = peer.Tag as BallUserSession;
    if (user == null) return;
    user.Color = req.Color;

    AuthAck ack = new AuthAck();
    ack.UserId = user.UserId;
    Console.WriteLine("用户发送登录请求 生成新的UserId {0}", ack.UserId);
    user.SendPacket<AuthAck>(EventId.IdAuthAck,0,ack);
    
    //NetDataWriter dataWriter = new NetDataWriter();
    //dataWriter.Reset();
    //dataWriter.Put(req.CalculateSize());

    //using (MemoryStream stream = new MemoryStream())
    //{
    //    ack.WriteTo(stream);
    //    dataWriter.PutBytesWithLength(stream.GetBuffer());
    //}
    //peer.Send(dataWriter, DeliveryMethod.Unreliable);
}

void onRoomListReq(PacketBase pb, NetPeer peer)
{
    Console.WriteLine("用户发送请求房间信息");
    //Console.WriteLine("payload id = {0} payload length = {1} payload str = ", EventId.IdRoomListReq.ToString(), pb.Payload.Length, BallServer.DebugDataStr(pb.Payload.ToByteArray()));
    RoomListReq req = RoomListReq.Parser.ParseFrom(pb.Payload);

    BallUserSession user = peer.Tag as BallUserSession;

    RoomListAck ack = new RoomListAck();
    
    List<Room> list = RoomMgr.Instance.RoomList();
    for(int i = 0;i < list.Count;i++)
    {
        RoomInfo info = new RoomInfo();
        info.RoomId = list[i].RoomId;
        info.IsGameing = list[i].IsGaming;
        for(int u = 0;u < list[i].Users.Count;u++)
        {
            UserInfo ui = list[i].Users[u].ToUserInfo();
            info.Users.Add(ui);
        }
        ack.Rooms.Add(info);
    }
    user.SendPacket<RoomListAck>(EventId.IdRoomListAck, 0, ack);
}

void onCreateRoomReq(PacketBase pb, NetPeer peer)
{
    //Console.WriteLine("payload id = {0} payload length = {1} payload str = ", EventId.IdCreateRoomReq.ToString(), pb.Payload.Length, BallServer.DebugDataStr(pb.Payload.ToByteArray()));
    CreateRoomReq req = CreateRoomReq.Parser.ParseFrom(pb.Payload);
    Console.WriteLine("用户请求创建新房间 房间名:{0}",req.RoomName);
    BallUserSession user = peer.Tag as BallUserSession;
    if (user == null) return;
    Room room = RoomMgr.Instance.CreateRoom(user);
    CreateRoomAck ack = new CreateRoomAck();
    ack.RoomId = room.RoomId;
    user.SendPacket<CreateRoomAck>(EventId.IdCreateRoomAck, 0, ack);
}


void onJoinRoomReq(PacketBase pb, NetPeer peer)
{
    JoinRoomReq req = JoinRoomReq.Parser.ParseFrom(pb.Payload);
    BallUserSession user = peer.Tag as BallUserSession;

    JoinRoomAck ack = new JoinRoomAck();
    ack.RoomInfo = new RoomInfo();
    ack.RoomInfo.RoomId = -1;
    Room room = RoomMgr.Instance.FindRoom(req.RoomId);
    if(room == null)
    {
        Console.WriteLine("找不到房间Id {0} ",req.RoomId);
        user.SendPacket<JoinRoomAck>(EventId.IdJoinRoomAck, -1, ack);
        return;
    }
    
    if (room.IsGaming)
    {
        Console.WriteLine("房间Id {0} 已经开始游戏" ,req.RoomId);
        user.SendPacket<JoinRoomAck>(EventId.IdJoinRoomAck, -1, ack);
        return;
    }

    if(room.IsExistUser(user))
    {
        Console.WriteLine("房间Id {0} UserId {1} 已经在房间里了", req.RoomId,user.UserId);
        user.SendPacket<JoinRoomAck>(EventId.IdJoinRoomAck, -1, ack);
        return;
    }
    

    OtherJoinRoomNotify notify = new OtherJoinRoomNotify();
    notify.UserInfo = user.ToUserInfo();
    room.BroadcastToUser(EventId.IdOtherJoinRoomNotify, 0, notify);

    RoomInfo info = new RoomInfo();
    info.RoomId = room.RoomId;
    for (int u = 0; u < room.Users.Count; u++)
    {
        BallUserSession other_user = room.Users[u];
        UserInfo ui =other_user.ToUserInfo();
        info.Users.Add(ui);
    }

    ack.RoomInfo = info;
    room.AddUser(user);
    user.SendPacket<JoinRoomAck>(EventId.IdJoinRoomAck, 0, ack);
    
}

void onExitRoomReq(PacketBase pb, NetPeer peer)
{
    ExitRoomReq req = ExitRoomReq.Parser.ParseFrom(pb.Payload);
    BallUserSession user = peer.Tag as BallUserSession;

    ExitRoomAck ack = new ExitRoomAck();
    
    Room room = RoomMgr.Instance.FindRoom(user.CurrentRoomId);
    if(room == null)
    {
        user.SendPacket<ExitRoomAck>(EventId.IdExitRoomAck, -1, ack);
        return;
    }
    room.RemoveUser(user);

    OtherExitRoomNotify notify = new OtherExitRoomNotify();
    notify.UserInfo =user.ToUserInfo();
    room.BroadcastToUser<OtherExitRoomNotify>(EventId.IdOtherExitRoomNotify, 0, notify);
}


void onReadyReq(PacketBase pb, NetPeer peer)
{
    ReadyReq req = ReadyReq.Parser.ParseFrom(pb.Payload);
    BallUserSession user = peer.Tag as BallUserSession;
    if (user == null) return;
    
    Room room = RoomMgr.Instance.FindRoom(user.CurrentRoomId);
    if (room == null)
    {
        Console.WriteLine("UserId {0} RoomId = {1} can't find room",user.UserId,user.CurrentRoomId);
        return;
    }
    Console.WriteLine("用户请求准备 {0}",req.IsReady);

    user.IsReady = req.IsReady;
    

    ReadyAck ack = new ReadyAck();
    user.SendPacket<ReadyAck>(EventId.IdReadyAck, 0, ack);

    OtherReadyNotify notify = new OtherReadyNotify();
    notify.UserInfo = user.ToUserInfo();
    room.BroadcastToUser(EventId.IdOtherReadyNotify, 0, notify);

    if (room.CheckUserAllReady())
    {
        room.RoomState = new RoomStateReady(room);
    }
}

void onStartGameAck(PacketBase pb,NetPeer peer)
{
    StartGameAck req = StartGameAck.Parser.ParseFrom(pb.Payload);
    BallUserSession user = peer.Tag as BallUserSession;
    if (user == null) return;

    Room room = RoomMgr.Instance.FindRoom(user.CurrentRoomId);
    if (room == null)
    {
        Console.WriteLine("UserId {0} RoomId = {1} can't find room", user.UserId, user.CurrentRoomId);
        return;
    }
    if(room.IsExistUser(user))
    {
        room.AddUserAck(user);
    }
}

void onUploadFrameData(PacketBase pb, NetPeer peer)
{
    UploadFrameData req = UploadFrameData.Parser.ParseFrom(pb.Payload);
    BallUserSession user = peer.Tag as BallUserSession;
    if (user == null) return;

    Room room = RoomMgr.Instance.FindRoom(user.CurrentRoomId);
    if (room == null)
    {
        Console.WriteLine("UserId {0} RoomId = {1} can't find room", user.UserId, user.CurrentRoomId);
        return;
    }
    for(int  i = 0;i <req.Cmds.Count;i++)
    {
        room.RoomState.OnReceiveCommand(req.Cmds[i]);
    }

    if (room.IsFrameCmdCollectAll(req.FrameIndex, room.RoomUserNum))
    {
        Console.WriteLine("收集完第{0}帧数据 开始同步给玩家", req.FrameIndex);
        room.BroadcastFrameDataToUser(req.FrameIndex);
    }
}