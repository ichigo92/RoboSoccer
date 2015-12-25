using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SSLRig.Core.Interface;

namespace SSLRig.Core.Threading
{
    /// <summary>
    /// A parallel processing execution engine utilizing Task Parallel Library. The Task[] provided by any single task are all
    /// executed in parallel
    /// </summary>
    class ParallelProcessor:IExecutionEngine
    {
        protected ITask[] _startingPorints;
        protected List<ITask> _nextTaskList;
        protected List<Task> _parallelsList;
        protected Task lastTask;
        protected Task currentTask;
        protected bool pause = false;
        protected bool isStopped = false;

        public void Initialize(ITask[] startPoints)
        {
            _nextTaskList = new List<ITask>(startPoints);
            _parallelsList = new List<Task>();
            _startingPorints = startPoints;
            currentTask = new Task(executeTasks);
            currentTask.ContinueWith(createNextTask);
        }

        protected void executeTasks()
        {
            if(isStopped)
                return;
            while (pause) ;
            if (lastTask != null)
            {
                lastTask.Dispose();
            }
            if (_nextTaskList.Count == 0)
            {
                _nextTaskList.AddRange(_startingPorints);
            }
            ITask[] tasks = _nextTaskList.ToArray();
            _nextTaskList.Clear();
            foreach (ITask task in tasks)
            {
                _parallelsList.Add(Task.Factory.StartNew(task.Execute));
                if (task.GetNext != null)
                {
                    ITask[] nextTasks = task.GetNext();
                    foreach (ITask nextTask in nextTasks)
                    {
                        if(nextTask!=null)
                            _nextTaskList.Add(nextTask);
                    }
                }
            }
            if (_parallelsList.Count > 0)
            {
                Task.WaitAll(_parallelsList.ToArray());
                foreach(Task task in _parallelsList)
                    task.Dispose();
                _parallelsList.Clear();
            }
            
        }

        protected void createNextTask(Task callee)
        {
            if(isStopped) return;
            lastTask = callee;
            currentTask = new Task(executeTasks);
            currentTask.ContinueWith(createNextTask);
            currentTask.Start();
        }

        public void Start()
        {
            if (!isStopped)
            {
                if (pause)
                    pause = false;
                else
                    currentTask.Start();
            }
            else
            {
                throw new ThreadStateException("The processor has been stopped. Re-Initialize it.");
            }
        }

        public void Pause()
        {
            pause = true;
        }

        public void Stop()
        {
            isStopped = true;
        }

        public void Dispose()
        {
            if (currentTask != null) currentTask.Dispose();
            if (lastTask != null) lastTask.Dispose();
            _nextTaskList.Clear();
            _nextTaskList = null;
        }
    }
}
