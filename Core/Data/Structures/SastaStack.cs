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
using System.Threading;

namespace SSLRig.Core.Data.Structures
{
    /// <summary>
    /// A generic circular stack implementation. 
    /// Discards the items older than the provided threshold, which are then cleaned by the GC.
    /// </summary>
    /// <typeparam name="T">The type of parameter to be stacked</typeparam>
    public class SastaStack<T>
    {
        protected int _totalObjects, _currentPosition = -1;
        protected T[] _objects;

        protected bool IsLocked;
        protected readonly object pLock = new object();

        public SastaStack() :this(2)
        { }

        public SastaStack(int totalobjects)
        {
            if (totalobjects < 1)
                throw new Exception("You cannot initialize it with less than one object limit. ");
            _totalObjects = totalobjects;
            _currentPosition = totalobjects - 1;
            _objects = new T[_totalObjects];
        }

        protected int CorrectedIndex(int index)
        {
            if ((index = index + _currentPosition) >= _totalObjects)
                index = index % _totalObjects;
            return index;
        }

        public T this[int index]
        {
            get
            {
                if (index >= _totalObjects)
                    throw new IndexOutOfRangeException(
                        "Your object doesn't exist. Probably because you've given an index greater than the maximum number of objects it can store. Choose an index lesser than " + _totalObjects.ToString() + ". ");
                try
                {
                    Monitor.Enter(pLock, ref IsLocked);
                    return _objects[CorrectedIndex(index)];
                }
                finally
                {
                    if (IsLocked)
                    {
                        Monitor.Exit(pLock);
                        IsLocked = false;
                    }
                }
            }
        }

        public int TotalObjects
        {
            get { return _totalObjects; }
        }

        public void Insert(T obj)
        {
            try
            {
                Monitor.Enter(pLock, ref IsLocked);
                _objects[_currentPosition--] = obj;
                if (_currentPosition < 0)
                    _currentPosition = _totalObjects - 1;
            }
            finally
            {
                if (IsLocked)
                {
                    Monitor.Exit(pLock);
                    IsLocked = false;
                }
            }

        }

        public T Get(int index)
        {
            return this[index];
        }

        public T GetLatest()
        {
            int virtualIndex = _currentPosition + 1;
            return _objects[virtualIndex == _totalObjects ? 0 : virtualIndex];
        }

        public T[] GetAll()
        {
            T[] all = new T[_totalObjects];

            for (int i = 0; i < _totalObjects; i++)
                all[i] = this[i];

            return all;
        }

        public bool IsEmpty()
        {
            foreach (T temp in _objects)
            {
                if (temp != null)
                    return false;
            }
            return true;
        }
    }
}
