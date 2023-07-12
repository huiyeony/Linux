using System;
namespace Server.Game
{
    public interface IJob
    {
        public void Execute();
    }
    //래핑 클래스

    public class Job :IJob
    {

        Action _action = null;

        public Job(Action action)
        {
            _action = action;//저장 
        }

        public void Execute()
        {
            _action.Invoke();
        }
    }
    public class Job<T1> : IJob
    {
        Action<T1> _action = null;
        T1 _t1;

        public Job(Action<T1> action, T1 t1)
        {
            _action = action;//저장
            _t1 = t1;
        }

        public void Execute()
        {
            _action.Invoke(_t1);
        }
    }
    public class Job<T1,T2> : IJob
    {
        Action<T1,T2> _action = null;
        T1 _t1;
        T2 _t2;

        public Job(Action<T1,T2> action, T1 t1,T2 t2)
        {
            _action = action;//저장
            _t1 = t1;
            _t2 = t2;
        }

        public void Execute()
        {
            _action.Invoke(_t1,_t2);
        }
    }


}

