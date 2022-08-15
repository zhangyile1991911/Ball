using BallNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallSrv
{
    public abstract class RoomState
    {
        protected Room m_controlRoom;
        public RoomState(Room room)
        {
            m_controlRoom = room;
        }
        public abstract void EnterState();

        public abstract void ExitState();
        
        public virtual void AddUser(BallUserSession user)
        {
            m_controlRoom.AddUser(user);
        }

        public virtual void RemoveUser(BallUserSession user)
        {
            m_controlRoom.RemoveUser(user);
        }

        public virtual void Update()
        {

        }
        public virtual void OnReceiveCommand(Command cmd)
        {

        }

    }

    public class RoomStateWaiting : RoomState
    {
        public RoomStateWaiting(Room room):base(room)
        {
            
        }

        public override void EnterState()
        {
            Console.WriteLine("RoomId {0} 进入等待阶段",m_controlRoom.RoomId);
        }

        public override void ExitState()
        {
            Console.WriteLine("RoomId {0} 退出等待阶段", m_controlRoom.RoomId);
        }
        public override void Update()
        {
            bool all_ready = m_controlRoom.CheckUserAllReady();
            if(all_ready)
            {
                m_controlRoom.RoomState = new RoomStateReady(m_controlRoom);
            }
        }
    }

    public class RoomStateReady : RoomState
    {
        public RoomStateReady(Room room):base(room)
        {

        }

        public override void EnterState()
        {
            Console.WriteLine("RoomId {0} 进入准备阶段 等待客户端反馈", m_controlRoom.RoomId);
            StartGameReq req = new StartGameReq();
            req.RoomId = m_controlRoom.RoomId;
            m_controlRoom.BroadcastToUser(EventId.IdStartGameReq, 0, req);
        }

        public override void ExitState()
        {
            Console.WriteLine("RoomId {0} 退出准备阶段 客户端反馈收集完成", m_controlRoom.RoomId);
        }

        public override void Update()
        {
            if(m_controlRoom.IsAllUserAck)
            {
                m_controlRoom.RoomState = new RoomStateGaming(m_controlRoom);
            }
        }
    }

    public class RoomStateGaming : RoomState
    {
        LogicTimer m_logicTimer;
        public RoomStateGaming(Room room):base(room)
        {
            m_logicTimer = new LogicTimer(logicTrigger);
            
        }

        void logicTrigger()
        {//todo触发 生成食物的消息

        }

        public override void EnterState()
        {
            Console.WriteLine("RoomId {0} 进入游戏阶段", m_controlRoom.RoomId);
            m_logicTimer.Start();
            StartGameReq req = new StartGameReq();
            req.RoomId = m_controlRoom.RoomId;
            m_controlRoom.BroadcastToUser(EventId.IdStartGameReq, 0, req);
        }

        public override void ExitState()
        {
            Console.WriteLine("RoomId {0} 退出游戏阶段", m_controlRoom.RoomId);
            m_logicTimer.Stop();
            m_controlRoom.ResetFrameDic();
            m_controlRoom.ResetUserAck();
        }

        //public override void OnReceiveCommands(List<Command> cmds)
        //{
        //    for(int  i = 0;i < cmds.Count;i++)
        //    {
        //        var one = cmds[i];
        //        m_controlRoom.AddFrame(one.FrameIndex, one.UserId, one);
        //    }
            
        //}

        public override void OnReceiveCommand(Command cmd)
        {
            m_controlRoom.AddFrame(cmd.FrameIndex, cmd.UserId, cmd);
            if(m_controlRoom.IsFrameCmdCollectAll(cmd.FrameIndex,m_controlRoom.RoomUserNum))
            {
                m_controlRoom.BroadcastFrameDataToUser(cmd.FrameIndex);
            }
        }

        public override void Update()
        {

        }
    }
}
