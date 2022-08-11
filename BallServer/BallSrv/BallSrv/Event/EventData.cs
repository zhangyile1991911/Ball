using System;
/*
where T : struct 限制类型参数T必须继承自System.ValueType。
where T : class 限制类型参数T必须是引用类型，也就是不能继承自System.ValueType。
where T : new() 限制类型参数T必须有一个缺省的构造函数
where T : NameOfClass 限制类型参数T必须继承自某个类或实现某个接口。
以上这些限定可以组合使用，比如： public class Point where T : class, IComparable, new()
*/
public abstract class EventData<T> : SharedValue<T> where T : class, new()
{
    public virtual void Clear()
    {

    }
}
