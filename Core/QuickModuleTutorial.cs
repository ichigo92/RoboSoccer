using System;
using System.Net;
using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Data.Structures;
using SSLRig.Core.Infrastructure.Communication;
using SSLRig.Core.Infrastructure.Control;
using SSLRig.Core.Intelligence.Planning;
using SSLRig.Core.Interface;
using SSLRig.Core.Threading;

namespace SSLRig.Core
{
    public class QuickModuleTutorial
    {
        
        private IPacketReceiver receiver; //= new SSLVisionReceiver(IPAddress.Parse("224.5.23.2"), 10002);
        //IPacketSender is an Interface, see?
        
        //A CommandSender to send commands to your robots.
        private IPacketSender sender;

        //Some controllers equivalent to the number of robots you have. These controllers generate velocities for each robot.
        private IController[] controllers;

        //A planner, which is where you write your logic to derive your robots.
        private IPlanner planner;

        //Another receiver to receive the referee commands form SSL-Vision
        private IPacketReceiver refereeReceiver;

        //Optionally, A DataSource where all the incomming/outgoing data will be stored.
        private IRepository dataRepository;

        //And optionally if you're going to use the task executor we provided.
        private IExecutionEngine executionEngine;

        //Now that we have some variables/references, we can start by declaring each one of them. So we do it here in the constructor
        public QuickModuleTutorial()
        {
            //Lets start with the receiver
            receiver = new SSLVisionReceiver(IPAddress.Parse("224.5.23.2"), 10020);
            //The constructor to receiver needs the multicast IP and port on which the SSL Vision server is multicasting its packets,
            //These can be found on SSL-Vision settings. Do make sure that your SSL-vision and network is correctly set up.
            //You can test this by first connecting the receiver
            receiver.Connect();
            //Then making a call on receive
            object packet = receiver.Receive();
            if(packet==null)
                throw new Exception("You messed up. Check your network and SSL Vision settings. ");
            
            //Next, we have the sender
            //Now we provide two kinds of senders, one for GRSim simulator, and another made for XBEE modules, initialize the one you need
            sender = new GRSimSender(IPAddress.Loopback, 10020);
            //Again, the IP address and port are the ones set up on GRSim. Verify them and that your network is good.
            //You can test the sender too, first connect it
            sender.Connect();
            //Now create a RobotParameters object and set some random values i.e.
            RobotParameters roboparams = new RobotParameters();
            roboparams.IsBlue = true;
            roboparams.Id = 1;
            roboparams.XVelocity = 2;
            //And send this packet through the sender
            sender.Send(roboparams);
            //If the robot specified in the robotId above doesn't show any movement, you messed up again.

            //If you need the XBEE sender, then initialize it as
            //sender = new XBeeSender("COM1");
            //where COM1 is the port you connected the XBEE module to
            //Ofcourse the XBEE sender needs the individual velocities to send them, so you need to set the angles between the wheels in each IRobotInfo's
            //WheelAngles property before sending the packet

            //Now comes the controllers, the one we provide is a PIDController, which is a modified implementation of PID by this guy Ron Beyer
            //Firstly, you need to initialize the array of the controllers with length equal to the number of robots you have, or you like to move
            //Lets say you are controlling six robots
            controllers = new PIDController[6];
            //next, initialize each controller with parameters of the robot
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i] = new PIDController(i, 2, 0, 0, 3025, -3025, 2, -2, 2, 0, 0, 2025, -2025, 2, -2, 5, 0, 0,
                    2*Math.PI, -2*Math.PI, Math.PI, -Math.PI);
                //Yes its a hell lot of arguments and we did give out a much smaller constructor with default values, but thats the problem, they're default and may not suit
                //your environment. Try to use this constructor. As for what the parameters are, I am not going to spill it out here, I've already given it in the constructor 
                //summary, go check there
            }

            //Now comes the module, which is completely your responsibility. Its the module that caused us to get into all this shit. Its the module that caused us to
            //remodel our original program to have only one button named "Deep Shit"
            //Yup you guessed it, the planner.
            //We're not giving out the complete intelligence planner yet even though it contains some basic calculation functions, 
            //we do provide a manual controller, which can be used with a Joystick, so connect a joystick
            //but for now, lets stick to some basics behavior, i.e. a test behavior defined in a dummy planner is to follow all the opponent robots
            //Initialize the dummy planner as follows
            planner = new DummyPlanner();

            //Now if you have a working SSL-Vision receiver, you can also safely declare a referee receiver, its not currently being used but you'll need it
            //when you write your own logic for obeying referee commands
            //refereeReceiver = new SSLRefereeReceiver(IPAddress.Parse("224.5.23.3"), 10003);

            //At this point, you have the absolute minimum things you need to run your system.
            //Now you can make individual calls to each of the modules again and agin like

            SSL_WrapperPacket receivedPacket = (SSL_WrapperPacket) receiver.Receive();
            if (receivedPacket != null)
            {
                IRobotInfo[] plannedPoints = planner.PlanExclusive(receivedPacket);
                if (plannedPoints != null)
                {
                    for (int i = 0; i < plannedPoints.Length; i++)
                    {
                        plannedPoints[i] = controllers[i].ComputeExclusive(plannedPoints[i],
                            receivedPacket.detection.robots_blue[plannedPoints[i].Id]);
                    }
                    foreach(var pointWithVelocity in plannedPoints)
                        sender.Send(pointWithVelocity);
                }
            }
            //and putting it in an infinite loop, which is pretty hideous if you ask me
            // or you can automate the process with the rest of useful architecture we provided, for example;
            
            //The data repository is a kind of utility we created to parse the incomming commands and store incomming/outgoing data in an organized way, 
            //its a good software practice so if you want to use it, you can initialize it too
            dataRepository = new DataRepository(3, true);
            //The repository also stores previous packets, which is useful for interpolation techniques. The size of history is given as the constructor argument
            //The repository can be plugged in each of the above modules as follows. The modules will automatically read/write on repository

            IDataSource sourceReference = (IDataSource) receiver;
            sourceReference.Repository = dataRepository;
            sourceReference = (IDataSource) sender;
            sourceReference.Repository = dataRepository;
            foreach (var controller in controllers)
            {
                sourceReference = (IDataSource) controller;
                sourceReference.Repository = dataRepository;
            }
            sourceReference = (IDataSource) planner;
            sourceReference.Repository = dataRepository;
            //To keep track of your game configuration, configure the data source too, i.e. add the id's of the robots that you are using. you can access it in your planner
            for (int i = 0; i < 6; i++)
                dataRepository.Configuration.AddRobot(i, null);
            //Its also advised to use the OnRefereeCommandChanged method to notify the planner whenever a change in referee command status occurs
            //This is done by assigning the method as follows
            dataRepository.OnRefereeCommandChanged += planner.OnRefereeCommandChanged;
            
            //And we provide execution engines, which, as the name says execute your tasks. All you have to do is define a sequence of tasks
            //First select an anchor task which acts as starting point and branches onto all other tasks.
            //Now each of the above modules also implements and ITask interface which is essentially what each ExecutionEngine uses
            //So, evidently, the sequence begins with reception of packet. So here's the anchor
            ITask anchor = (ITask) receiver;

            //To build the sequence of what comes next, we assign a delegate to the GetNext property of the anchor. The delegate returns an array of tasks,
            //These tasks are the ones the engine will perform after the anchor's task has been performed.
            //The next task should be to plan, so
            anchor.GetNext = () => new[] {(ITask) planner};

            //I've used the fancy lambda expression. It may look berserk but once you get to understand them they're quite handy
            //Alternately, you can create a method with a signature matching the GetNextTasks, and in its body, return an array of ITask, i.e. the tasks you want performed next
            //This is how:
            //
            //public ITask[] plannerReturner()
            //{
            //    ITask[] array = new ITask[1];
            //    array[0] = (ITask) planner;
            //    return array;
            //}
            //
            //And assign it
            //anchor.GetNext = new GetNextTasks(plannerReturner);

            //Next we take the planner and assign it a next sequence of tasks, which would be the controllers
            ITask anotherReference = (ITask) planner;
            anotherReference.GetNext = () =>
            {
                ITask[] controllerTasks = new ITask[controllers.Length];
                for (int i = 0; i < controllers.Length; i++)
                    controllerTasks[i] = (ITask) controllers[i];
                return controllerTasks;
            };

            //Next the controllers need to queue the tasks to send the data
            foreach (var controller in controllers)
            {
                anotherReference = (ITask) controller;
                anotherReference.GetNext = () => new[] {(ITask) sender};
            }

            //and the sender is the terminal
            //Now lets bring in the execution engine, i'm going to go with the single processor, parallel processor's kinda experimental. Feel free to play with it if you want
            executionEngine = new SingleProcessor(new[] {anchor});
            //The processor takes an array of starting points/anchors.
            //Now all you have to di is start the engine
            executionEngine.Start();

            //And voila, you're done. Each module's ITask interface exposes the Execute() method which is called by the processor. 
            //Remember how we plugged in a repository to each module, each module's Execute() takes care of taking data and writing it.
            //Ofcourse, to keep this model working, I would advise you to follow the design pattern here. i.e. If you create a module 
            //by yourself according to your needs, it needs to implement one of the module specific interfaces e.g. IPacketSender for
            //anything that sends outgoing packets, IPlanner for strategy planning/path planning modules, IController for any other 
            //position to velocity generation controller. The module must also implement two additional interfaces i.e. ITask and IDataSource
            //after which the module can easily be plugged in this model.

            //This project is a work in progress, and also contains a Rig module that has all of the above done in it, and a GUI that
            //can completely configure and manage multiple Rigs. We hope to complete it soon. This tutorial is an absolute basic,
			//you can navigate to different classes for better understanding. Feel free to ask us if you don't understand anything about
			//this project, YES ONLY THIS PROJECT, don't bother us if you don't know how to work around events, or if you don't understand
			//the concept of Threads, References or environment specific stuff. Try searching for these before.

            //Finally, we're all humans and we all make mistakes. This thing is not error proof, heck its not even tested properly.
            //So if you do find any bugs do feel free to report them to us so we can reflect it here. Remember, the basic purpose is
            //to learn, so don't be selfish. We're kind enough to give our work out to you, be kind yourselves. 
            //Also take into consideration to contribute to this project and become an active member of something that might help a lot
            //of generations to come.
        }

    }
}
