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

using System;
using SSLRig.Core.Common;
using SSLRig.Core.Interface;

namespace SSLRig.Core
{
    /// <summary>
    /// Under construction
    /// </summary>
    public class Rig
    {
        protected IRepository _repository;
        protected IPacketReceiver _visionReceiver;
        protected IPacketReceiver _refereeReceiver;
        protected IPlanner _mainPlanner;
        protected IController[] _controllers;
        protected IPacketSender _commandSender;
        protected IExecutionEngine _processor;

        public Rig() { }

        public void Wire()
        {
            if(_repository==null) throw new NullReferenceException("The repository has not been initialized. ");
            IDataSource wrapper = null;
            ITask taskWrapper = null;
            if(_visionReceiver==null)
                throw new NullReferenceException("The packet receiver has not been configured. ");
            if(!VerifyForDataSource(_visionReceiver))
                throw new NotImplementedException("The "+ _visionReceiver.GetType().ToString() +" class does not implement the IDataSource interface. ");
            wrapper = (IDataSource)_visionReceiver;
            wrapper.Repository = _repository;
            if (!VerifyForTask(_visionReceiver))
                throw new NotImplementedException("The " + _visionReceiver.GetType().ToString() +
                                                  "class does not implement the ITask interface. ");
            taskWrapper = (ITask) _visionReceiver;
            taskWrapper.GetNext = new GetNextTasks(PlannerBranch);

            if(_refereeReceiver==null)
                throw new NullReferenceException("The referee packet receiver has not been configured. ");
            if (!VerifyForDataSource(_refereeReceiver))
                throw new NotImplementedException("The " + _refereeReceiver.GetType().ToString() + " class does not implement the IDataSource interface. ");
            wrapper = (IDataSource) _refereeReceiver;
            wrapper.Repository = _repository;
            if(_mainPlanner ==null)
                throw new NullReferenceException("The planner has not been configured. ");
            if (!VerifyForDataSource(_mainPlanner))
                throw new NotImplementedException("The "+_mainPlanner.GetType().ToString()+" class does not implement the IDataSource interface. ");
            wrapper = (IDataSource) _mainPlanner;
            wrapper.Repository = _repository;
            if (!VerifyForTask(_mainPlanner))
                throw new NotImplementedException("The " + _mainPlanner.GetType().ToString() +
                                                  "class does not implement the ITask interface. ");
            taskWrapper = (ITask)_mainPlanner;
            taskWrapper.GetNext = new GetNextTasks(ControllerBranch);

            if(_controllers==null)
                throw new NullReferenceException("The controllers have not been configured. ");
            foreach (var controller in _controllers)
            {
                if(controller==null)
                    throw new NullReferenceException("One of the controllers has not been initialized." );
                if(!VerifyForDataSource(controller))
                    throw new NotImplementedException("The " + controller.GetType().ToString() + " class does not implement the IDataSource interface. ");
                wrapper =(IDataSource) controller;
                wrapper.Repository = _repository;

                if (!VerifyForTask(controller))
                    throw new NotImplementedException("The " + controller.GetType().ToString() +
                                                      "class does not implement the ITask interface. ");
                taskWrapper = (ITask)controller;
                taskWrapper.GetNext = new GetNextTasks(SenderBranch);
            }
            if (_commandSender == null)
                throw new NullReferenceException("The command sender has not been configured. ");
            if (!VerifyForDataSource(_commandSender))
                throw new NotImplementedException("The " + _commandSender.GetType().ToString() + " class does not implement the IDataSource interface. ");
            wrapper = (IDataSource)_commandSender;
            wrapper.Repository = _repository;
            _processor.Initialize(new[] {(ITask) _visionReceiver, (ITask) _refereeReceiver});
        }

        public bool IsReady
        {
            get
            {
                if (_repository == null)
                    return false;
                else if (!_repository.Configuration.IsConfigured)
                    return false;
                else if (_visionReceiver == null)
                    return false;
                else if (_visionReceiver == null)
                    return false;
                else if (_mainPlanner == null)
                    return false;
                else if (_controllers == null)
                    return false;
                else if (_commandSender == null)
                    return false;
                else if (_processor == null)
                    return false;
                else
                {
                    return true;
                }
            }
        }

        public void Start()
        {
            if(IsReady)
                _processor.Start();
            else throw new Exception("The Rig has not been set-up properly. ");
        }

        public void Pause()
        {
            _processor.Pause();
        }

        public void Stop()
        {
            _processor.Stop();
        }

        public IPacketReceiver VisionReceiver
        {
            get { return _visionReceiver; }
            set { _visionReceiver = value; }
        }

        public IPacketReceiver RefereeReceiver
        {
            get { return _refereeReceiver; }
            set { _refereeReceiver = value; }
        }

        public IRepository Repository
        {
            get { return _repository; }
            set { _repository = value; }
        }

        public IPlanner Planner
        {
            get { return _mainPlanner; }
            set { _mainPlanner = value; }
        }

        public IController[] Controllers
        {
            get { return _controllers; }
            set { _controllers = value; }
        }

        public IPacketSender CommandSender
        {
            get { return _commandSender; }
            set { _commandSender = value; }
        }

        public IExecutionEngine Processor
        {
            get { return _processor; }
            set { _processor = value; }
        }

        protected bool VerifyForDataSource(object obj)
        {
            if (obj == null)
                return false;
            else
                return obj is IDataSource ? true : false;
        }

        protected bool VerifyForTask(object obj)
        {
            if (obj == null)
                return false;
            else return obj is ITask ? true : false;
        }

        protected ITask[] PlannerBranch()
        {
            return new[] {(ITask)this._mainPlanner};
        }

        protected ITask[] ControllerBranch()
        {
            return (ITask[]) _controllers;
        }

        protected ITask[] SenderBranch()
        {
            return new[] {(ITask) _commandSender};
        }
    }
}
