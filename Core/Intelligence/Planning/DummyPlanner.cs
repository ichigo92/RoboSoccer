using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Interface;
using System.Threading;
using System.Drawing;


namespace SSLRig.Core.Intelligence.Planning
{
    /// <summary>
    /// A dummy planner to perform fixed tasks
    /// </summary>
    public class DummyPlanner : IPlanner, ITask, IDataSource
    {
        protected IRepository repo;
        protected GetNextTasks getNextTasks;
        protected SSL_Referee refereeCommand;

        public IRepository Repository
        {
            get { return repo; }
            set { repo = value; }
        }

        public void Initialize()
        {
        }

        public void Plan()
        {
            //FollowOpponent();

            SSL_DetectionRobot[] _robots = repo.InData.Own();
            int check = 1;
            if (check == 1)
            {
                repo.OutData[0].SetPoint(_robots[0].x, _robots[0].y, _robots[0].orientation);
                repo.OutData[1].SetPoint(_robots[1].x, _robots[1].y, _robots[1].orientation);
            }
            check++;
         
             FollowBall();
           // newAngle();
           // dummyAngle();
        }

        public IRobotInfo[] PlanExclusive(Data.Packet.SSL_WrapperPacket mainPacket)
        {
            return null;
        }

        public void Release()
        {

        }

        public Common.GetNextTasks GetNext
        {
            get { return getNextTasks; }
            set { getNextTasks = value; }
        }

        public void Execute()
        {
            Plan();
        }

        public void OnRefereeCommandChanged(Data.Packet.SSL_Referee command)
        {
            refereeCommand = command;
        }

        #region Behaviors

        public void FollowOpponent()
        {
            foreach (var robot in repo.InData.Opponent())
            {
                if (robot != null)
                {
                    repo.OutData[(int)robot.robot_id].SetPoint(robot.x, robot.y, robot.orientation);
                }
            }
        }

        //Should i change the name of this method??
        double newAngle(int id)
        {
            SSL_DetectionBall[] _balls = repo.InData.GetBalls();
            SSL_DetectionRobot[] _robot=repo.InData.Own();
            //making our robot condinates as our origin of the field
            PointF robot_0 = new PointF(_robot[id].x, _robot[id].y);
            PointF robot = new PointF((_robot[id].x - (_robot[id].x)), (_robot[id].y - (_robot[id].y)));
            PointF balls = new PointF((_balls[0].x-(_robot[id].x)),( _balls[0].y-(_robot[id].y)));
            //making our robot condinates as our origin of the field
                double thetha = Math.Atan(balls.Y /balls. X);
                //Console.WriteLine("   Angle  "+RadianToDegree(thetha));
                if ((balls.X < 0 && balls.Y < 0) || (balls.X < 0 && balls.Y > 0))
                {
                    thetha += 3.14;
                    return thetha;
                 }
                else
                {
                    return thetha;
                }
        }

        #region Convert_rad_to_degree_and_degree_to_rad
        
        public static float DegreeToRadian(float degree)
        {
            float rad;
            rad = (float)(degree * Math.PI / 180);
            return rad;
        }

        public float RadianToDegree(double rad)
        {
            float degree;
            degree = (float)(rad * 180/Math.PI);
            return degree;
        }

        #endregion
        
        public void fieldCalculator()
        {
            SSL_DetectionRobot[] _robots = repo.InData.Own();
            Console.WriteLine("   Robot x   " + _robots[1].x + "  Robot y   " + _robots[1].y);
            SSL_DetectionBall[] _balls = repo.InData.GetBalls();
            repo.OutData[0].SetPoint(_balls[0].x, _balls[0].y, -4.71239F);
            Console.WriteLine(_robots[0].orientation);
            repo.OutData[1].SetPoint(2000, 2000, (4.71239F + 0.785398F));
        }


        public void FollowBall()
        {
            
            SSL_DetectionBall[] _balls = repo.InData.GetBalls();
            SSL_DetectionRobot[] _robots = repo.InData.Own();
          
            PointF balls = new PointF(_balls[0].x, _balls[0].y);
            PointF robots = new PointF(_robots[0].x, _robots[0].y);
            double _distanceZero = DistanceBetweenTwoPoints(balls, robots);

            robots.X = _robots[1].x;
            robots.Y = _robots[1].y;
            //repo.OutData[0].WVelocity = (float)30;
            double _distanceOne = DistanceBetweenTwoPoints(balls, robots);
            if (_distanceZero > _distanceOne)
            {
                float angle = (float)newAngle(1);
                repo.OutData[1].Grab = true;
                //repo.OutData[1].KickSpeed = 4;
                repo.OutData[1].Spin = true;
                repo.OutData[1].SetPoint(_balls[0].x - 105F, _balls[0].y, angle);
            }
            else
            {
                float angle = (float)newAngle(0);
                repo.OutData[0].Grab = true;
                repo.OutData[0].Spin = true;
                repo.OutData[0].SetPoint(_balls[0].x - 105F, _balls[0].y,angle);  
            }

        }

        public double DistanceBetweenTwoPoints(PointF balls, PointF robots)
        {
            double distance = Math.Sqrt((Math.Pow((balls.X - robots.X), 2) + Math.Pow((balls.Y - robots.Y), 2)));
            Console.WriteLine("Ball X: " + balls.X + " Ball Y: " + balls.Y);
            Console.WriteLine("Robot X: " + robots.X + " Robot Y: " + robots.Y);
            Console.WriteLine("Calculated Distance: " + distance);
            return distance;
        }


        public void Pass(SSL_DetectionRobot user, SSL_DetectionRobot partner)
        {

            if (user.y == partner.y && user.x < partner.x)
            {
                user.orientation = DegreeToRadian(180);
                repo.OutData[(int)user.robot_id].KickSpeed = 3;
            }
            else
            {
                Console.WriteLine("User Y: " + user.y);
                Console.WriteLine("Partner Y: " + partner.y);
                Console.WriteLine("User X: " + user.y);
                Console.WriteLine("Partner X: " + partner.x);
            }

        }

        #endregion
    }
}

