using System;
using System.Collections.Generic;

public static class EventDispatch
{
    static Dispatcher mDispatcher = new Dispatcher();

    public static bool RegisterReceiver<T>(int varEventID, Action<T> action)
    {
        return mDispatcher.RegisterReceiver(varEventID, action);
    }
    public static void UnRegisterReceiver<T>(int varEventID, Action<T> action)
    {
        mDispatcher.UnRegisterReceiver(varEventID, action);
    }

    public static void Dispatch<T>(int varEventID, T data)
    {
        mDispatcher.Dispatch(varEventID, data);
    }

    public static Type GetType(int varEventID)
    {
        return mDispatcher.GetType(varEventID);
    }

    public static void Clear()
    {
        mDispatcher.Clear();
    }
}

public static class MsgDispatch
{
    static MDispatcher mDispatcher = new MDispatcher();

    public static bool RegisterReceiver<T,P>(int varEventID, Action<T,P> action)
    {
        return mDispatcher.RegisterReceiver(varEventID, action);
    }
    public static void UnRegisterReceiver<T, P>(int varEventID, Action<T, P> action)
    {
        mDispatcher.UnRegisterReceiver(varEventID, action);
    }

    public static void Dispatch<T, P>(int varEventID, T data,P peer)
    {
        mDispatcher.Dispatch(varEventID, data, peer);
    }

    public static Type GetType(int varEventID)
    {
        return mDispatcher.GetType(varEventID);
    }

    public static void Clear()
    {
        mDispatcher.Clear();
    }
}

