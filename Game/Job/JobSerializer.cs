using System;
using System.Collections.Generic;

namespace Server.Game
{
    //job 관리 하는 주인님  
    public class JobSerializer
    {
        JobTimer _timer = new JobTimer();//나만의 시계?

        //자동 job 생성 함수 
        public void PushAfter(int tickAfter ,Action action) { PushAfter(tickAfter ,new Job(action)); }
        public void PushAfter<T1>(int tickAfter,Action<T1> action, T1 t1) { PushAfter(tickAfter ,new Job<T1>(action, t1)); }
        public void PushAfter<T1, T2>(int tickAfter, Action<T1, T2> action, T1 t1, T2 t2) { PushAfter(tickAfter , new Job<T1, T2>(action, t1, t2)); }

        public void PushAfter(int tickAfter,IJob job)
        {
            lock (_lock)
            {
                _timer.Push(job, tickAfter);
            }
        }
        public void FlushAfter()
        {
            lock (_lock)
            {
                _timer.Flush();
            }
        }

        Queue<IJob> _jobQueue = new Queue<IJob>();
        object _lock = new object();
      
        //자동 job 생성 함수 
        public void Push(Action action) { Push(new Job(action)); }
        public void Push<T1>(Action<T1> action, T1 t1) { Push(new Job<T1>(action, t1)); }
        public void Push<T1, T2>(Action<T1, T2> action, T1 t1, T2 t2) { Push(new Job<T1, T2>(action, t1, t2)); }

        public void Push(IJob job)
        {

            lock (_lock)
            {
                _jobQueue.Enqueue(job);
                
            }

        }

        public void Flush()
        {
            FlushAfter();//나만의 시계 

            while (true)
            {
                IJob job = Pop();
                if (job == null)
                    return;

                job.Execute();
            }
        }

        IJob Pop()
        {
            lock (_lock)
            {
                if (_jobQueue.Count == 0)
                {
                    
                    return null;
                }
                return _jobQueue.Dequeue();
            }
        }
    }
}

