using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BallSrv
{
    public class BallUserSessionMgr
    {
        private static BallUserSessionMgr m_instance;
        public static BallUserSessionMgr Instance
        {
            get
            {
                if (m_instance == null)
                    m_instance = new BallUserSessionMgr();
                return m_instance;
            }
        }
        private int m_count;
        public int IDGenerator
        {
            get { return ++m_count; }
        }
        private Dictionary<int, BallUserSession> m_user_dict;
        public BallUserSessionMgr()
        {
            m_user_dict = new Dictionary<int, BallUserSession>();
        }

        public void AddUser(BallUserSession user_sess)
        {
            if (user_sess == null) return;

            m_user_dict[user_sess.UserId] = user_sess;
        }

        public void RemoveUser(int uid)
        {
            if(m_user_dict.ContainsKey(uid))
            {
                m_user_dict.Remove(uid);
            }
        }

        public BallUserSession FindUser(int uid)
        {
            if (m_user_dict.ContainsKey(uid)) return m_user_dict[uid];
            return null;
        }

    }
}
