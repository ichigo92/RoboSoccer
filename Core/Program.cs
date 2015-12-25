using System;
using System.Collections.Generic;
using System.Net;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Data.Structures;
using SSLRig.Core.Infrastructure.Communication;
using SSLRig.Core.Infrastructure.Control;
using SSLRig.Core.Intelligence.Planning;
using SSLRig.Core.Interface;
using SSLRig.Core.Threading;

namespace SSLRig.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {


            IRepository repository = new DataRepository(5, true, false);
           // IPacketReceiver receiver = new SSLVisionReceiver(IPAddress.Parse("224.5.23.2"), 10002);
           // IPacketSender sender = new GRSimSender(IPAddress.Parse("20.200.20.207"), 20011);
            IPacketReceiver receiver = new SSLVisionReceiver(IPAddress.Parse("224.5.23.2"), 10020);
            IPacketSender sender = new GRSimSender(IPAddress.Parse("192.168.0.3"), 20011);
            receiver.Connect();
            sender.Connect();
            ((IDataSource)receiver).Repository = repository;
            ((IDataSource)sender).Repository = repository;
            IPlanner planner = new DummyPlanner();
            ((IDataSource)planner).Repository = repository;
            IController[] controllers = new IController[6];
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i] = new PIDController(i);
                ((IDataSource)controllers[i]).Repository = repository;
            }

            //repository.InData.ParsePacket((SSL_WrapperPacket)receiver.Receive());
            //planner.Plan();
            //controllers[0].Compute();
            //((ITask)sender).Execute();
            ITask anchor = (ITask)receiver;
            ITask reference = (ITask)planner;
            anchor.GetNext = () => new[] { reference };
            reference.GetNext = () =>
            {
                List<ITask> controller = new List<ITask>();
                foreach(ITask contr in controllers)
                    controller.Add(contr);
                return controller.ToArray();
            };
            foreach (var controller in controllers)
            {
                ((ITask)controller).GetNext = () => new[] { (ITask)sender };
            }

            IExecutionEngine engine = new SingleProcessor(new[] { anchor });
            engine.Start();
        }
    }
}
