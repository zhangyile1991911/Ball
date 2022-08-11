using BallNet;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallSrv
{
    public class Room
    {
        public RoomState RoomState
        {
            get { return _state; } 
            set
            {
                if(_state != null)
                {
                    _state.ExitState();
                }
                _state = value;
                _state.EnterState();
            }
        }
        private RoomState _state;
        public bool IsGaming { get { return isGaming; } }
        public int RoomUserNum { get { return user_list.Count; } }
        public int RoomId { get {return m_room_id;} }
        private int m_room_id;
        public List<BallUserSession> Users { get { return user_list; } }
        private List<BallUserSession> user_list;
        private BallUserSession m_owner;
        private bool isGaming;
        public bool IsAllUserAck { get 
            { 
                if(user_ack_set == null)
                {
                    return false;
                }
                return user_ack_set.Count == user_list.Count;
            } }
        private HashSet<int> user_ack_set;


        private Dictionary<long, Dictionary<int,List<Command>>> mFrameDic;//Dictionary<帧号,Dictionary<玩家id,操作列表>>
        public Room(int id,BallUserSession owner)
        {
            m_room_id = id;
            m_owner = owner;
            m_owner.JoinRoom(id);
            user_list = new List<BallUserSession>();
            user_list.Add(m_owner);
        }
        public void ResetUserAck()
        {
            user_ack_set = null;
        }
        public void ResetFrameDic()
        {
            mFrameDic = new Dictionary<long, Dictionary<int, List<Command>>>();
        }
        public void AddFrame(long frameIndex,int userId,Command cmd)
        {
            Dictionary<int, List<Command>> current_frame_dict;
            if (!mFrameDic.ContainsKey(frameIndex))
            {
                mFrameDic[frameIndex] = new Dictionary<int, List<Command>>();
            }
            current_frame_dict = mFrameDic[frameIndex];

            List<Command> user_frame_cmd;
            if(!current_frame_dict.ContainsKey(userId))
            {
                current_frame_dict[userId] = new List<Command>();
            }
            user_frame_cmd = current_frame_dict[userId];
            user_frame_cmd.Add(cmd);
        }
        public Dictionary<int, List<Command>> GetFrameCommand(long frameIndex)
        {
            if(mFrameDic.ContainsKey(frameIndex))
            {
                return mFrameDic[frameIndex];
            }
            return null;
        }

        public bool IsFrameCmdCollectAll(long frameIndex,int num)
        {
            if(mFrameDic.ContainsKey(frameIndex))
            {
                return mFrameDic[frameIndex].Count >= num;
            }
            return false;
        }

        public bool CheckUserAllReady()
        {
            bool all_ready = true;
            foreach(var one in user_list)
            {
                if(!one.IsReady)
                {
                    all_ready = false;
                    break;
                }
            }
            return all_ready;
        }
        public void AddUser(BallUserSession user)
        {
            if(user != null)
            {
                user.JoinRoom(m_room_id);
                user_list.Add(user);
            }
        }

        public void RemoveUser(BallUserSession user)
        {
            if(user != null)
            {
                user.ExitRoom();
                user_list.Remove(user);
            }
        }

        public bool IsExistUser(BallUserSession user)
        {
            if (user != null)
            {
                foreach(var one in user_list)
                {
                    if (one.UserId == user.UserId)
                        return true;
                }
            }
            return false;
        }

        public void AddUserAck(BallUserSession user)
        {
            if(user_ack_set == null)
            {
                user_ack_set = new HashSet<int>();
            }
            if(user_ack_set.Contains(user.UserId))
            {
                return;
            }
            user_ack_set.Add(user.UserId);
        }
        
        public void BroadcastToUser<T>(EventId id, int result, T msg, bool isReliable = true) where T : IMessage<T>
        {
            foreach(var one in user_list)
            {
                one.SendPacket<T>(id, result, msg, isReliable);
            }
        }
    }

    public class RoomMgr
    {
        private static RoomMgr m_instance;
        public static RoomMgr Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new RoomMgr();
                return m_instance;
            }
        }

        private int m_counter;
        private Dictionary<int, Room> m_room_dict;

        private RoomMgr()
        {
            m_room_dict = new Dictionary<int, Room>();
        }

        public Room CreateRoom(BallUserSession creator)
        {
            Room r = new Room(++m_counter, creator);
            r.RoomState = new RoomStateWaiting(r);
            m_room_dict.Add(r.RoomId,r);
            return r;
        }

        public Room FindRoom(int room_id)
        {
            if(m_room_dict.ContainsKey(room_id))
            {
                return m_room_dict[room_id];
            }
            return null;
        }
        public List<Room> RoomList()
        {
            if(m_room_dict.Count <= 0)
            {
                return new List<Room>();
            }
            return m_room_dict.Values.ToList();
        }

        public void UpdateRoom()
        {
            foreach(var one in m_room_dict.Values)
            {
                one.RoomState.Update();
            }
        }
    }
}
