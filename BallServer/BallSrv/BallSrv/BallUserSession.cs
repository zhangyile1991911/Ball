using Google.Protobuf;
using LiteNetLib;
using LiteNetLib.Utils;
using BallNet;
using System.IO;
using System;

namespace BallSrv
{
    public class BallUserSession
    {
        public int UserId{ get { return m_user_id; } }
        private int m_user_id;
        public bool IsReady { get { return m_ready; }set { m_ready = value; } }
        private bool m_ready;
        public int Color { get { return m_color; }set { m_color = value; } }
        private int m_color;
        public NetPeer Peer { get { return m_peer; } }
        private NetPeer m_peer;
        private int m_room_id;
        public int CurrentRoomId { get { return m_room_id; } }

        private NetDataWriter m_writer;
        public BallUserSession(NetPeer peer,int user_id)
        {
            m_peer = peer;
            m_user_id = user_id;
            m_room_id = -1;
            m_ready = false;
            m_writer = new NetDataWriter();
            m_peer.Tag = this;
        }

        public void JoinRoom(int room_id)
        {
            if(m_room_id < 0)
            {
                m_room_id = room_id;
            }
        }

        public void ExitRoom()
        {
            if(m_room_id > 0)
            {
                m_room_id = -1;
            }
        }

        public UserInfo ToUserInfo()
        {
            UserInfo info = new UserInfo();
            info.UserId = UserId;
            info.Ready = IsReady;
            info.Color = Color;
            return info;
        }

        public void SendPacket<T>(EventId id,int result,T msg,bool isReliable=true)where T:IMessage<T>
        {
            PacketBase pb = new PacketBase();
            pb.EventId = id;
            pb.Result = result;
            m_writer.Reset();
            
            using (MemoryStream stream = new MemoryStream())
            {
                msg.WriteTo(stream);
                pb.Payload = ByteString.CopyFrom(stream.ToArray());
               // Console.WriteLine("UserId {0} Send Payload = {1}",UserId,pb.Payload.ToByteArray());
                stream.Flush();

                pb.WriteTo(stream);
                m_writer.PutBytesWithLength(stream.ToArray());
                //BallServer.DebugData(id.ToString(),stream.ToArray());
                stream.Flush();
            }
            if(isReliable)
            {
                m_peer.Send(m_writer, DeliveryMethod.ReliableOrdered);
            }
            else
            {
                m_peer.Send(m_writer, DeliveryMethod.Unreliable);
            }
        }
    }
}
