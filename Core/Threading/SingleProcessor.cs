//    SSLRig - Small Size League Robot Integration Gadget
//    Copyright (C) 2015, Usman Shahid, Umer Javaid, Musaub Shaikh

//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.

//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.

//    You should have received a copy of the GNU General Public License
//    along with this program. If not, see <http://www.gnu.org/licenses/>.

using System.Collections.Concurrent;
using System.Threading;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Threading
{
    /// <summary>
    /// A single thread implementation of the IExecutionEngine, the implementation mantains a queue of tasks to be performed.
    /// The tasks added to the queue are specified by the tasks that have been executed.
    /// The implementation keeps with itself a task as the origin point. The last task must not give the initial task as the
    /// bext task as the stack may grow exponentialy
    /// </summary>
    public class SingleProcessor : IExecutionEngine
    {
        protected volatile bool _isRunning = false, _shouldStop = true;
        protected Thread _mainThread;
        protected ConcurrentQueue<ITask> _executionQueue;
        protected ITask[] _startPoint;
        protected int _sleepTime = 10;

        public int SleepTime
        {
            get { return _sleepTime; }
            set { _sleepTime = value; }
        }

        public SingleProcessor(ITask[] start)
        {
            Initialize(start);
        }

        public void Initialize(ITask[] startPoint)
        {
            _executionQueue = new ConcurrentQueue<ITask>();
            _startPoint = startPoint;
            _isRunning = false;
            _shouldStop = false;
            foreach (ITask task in startPoint)
                _executionQueue.Enqueue(task);
            _mainThread = new Thread(Process);
        }

        public void Start()
        {
            if (_startPoint == null)
                throw new ThreadStateException("No start task provided. ");
            if (_shouldStop)
                throw new ThreadStateException("The engine needs to be reinitialized. ");
            _isRunning = true;
            _mainThread.Start();
        }

        public void Pause()
        {
            _isRunning = false;
        }

        public void Stop()
        {
            Pause();
            _shouldStop = true;

        }

        public void Dispose()
        {
            Stop();
            _mainThread = null;
        }

        protected void Process()
        {
            bool queueStatus;
            ITask executionTask;
            while (!_shouldStop)
            {
                if (!_isRunning)
                {
                    Thread.Sleep(_sleepTime);
                    continue;
                }
                if (_executionQueue.IsEmpty)
                    foreach (ITask task in _startPoint)
                        _executionQueue.Enqueue(task);
                queueStatus = _executionQueue.TryDequeue(out executionTask);
                if (queueStatus)
                {
                    executionTask.Execute();
                    if (executionTask.GetNext != null)
                    {
                        foreach (ITask task in executionTask.GetNext())
                            _executionQueue.Enqueue(task);
                    }
                }
            }
        }
    }
}
