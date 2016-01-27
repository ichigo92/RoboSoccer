

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
//using SSLRig.Proto;
using System.Threading;
using SSLRig.Core.Common;
using SSLRig.Core.Data.Packet;
using SSLRig.Core.Interface;
using SSLRig.Core.Data.Structures;

namespace SSLRig.Core.Intelligence.Planning
{

    public class AIPlanner : IPlanner, ITask, IDataSource
    {
        //public static string rebuff;
        private PointF OwnGoal, OppGoal;
        public bool DynamicRoleAssignment;
        public bool DynamicTeamSideDetection;
        public int OwnGoalkeeperId = 0, OwnAttackerId = 0, OwnAttackSupportId = 0, OwnDefender1Id = 7, OwnDefender2Id = 7, OwnMarkerId = 0, OwnFreeRoleId;
        public int OppGoalkeeperId = 0, OppAttackerId = 0, OppAttackSupportId = 0, OppDefender1Id = 7, OppDefender2Id = 7, OppMarkerId = 0, OppFreeRoleId;
        public float OwnClosestRobotDistance = float.PositiveInfinity, OppClosestRobotDistance = float.PositiveInfinity, ShootingAngle;
        PointF OwnRobotsMean, OwnRobotsStandardDeviation, OppRobotsMean, OppRobotsStandardDeviation;

        IRepository repo;
        SSL_Referee refereCommand;
        GetNextTasks nextTask;

        #region Constructor
        public AIPlanner()
        {
            if (repo.Configuration.IsPlayingFromLeft)
            {
                OwnGoal.X = -3025;
                OwnGoal.Y = 0;
                OppGoal.X = 3025;
                OppGoal.Y = 0;
            }
            else
            {
                OwnGoal.X = 3025;
                OwnGoal.Y = 0;
                OppGoal.X = -3025;
                OppGoal.Y = 0;
            }
            DynamicRoleAssignment = true;
            DynamicTeamSideDetection = false;
        }
        #endregion

        #region Planning_Functions
        public void RoleAssignments()
        {
            List<SSL_DetectionRobot> OwnRobots;
            List<SSL_DetectionRobot> OppRobots;
            if (repo.Configuration.IsBlueTeam)     //extract robot info and store in blue/yellow robots for role classification
            {
                OwnRobots = repo.InData.Own().ToList<SSL_DetectionRobot>();
                OppRobots = repo.InData.Opponent().ToList<SSL_DetectionRobot>();
            }
            else                        //extract robot info and store in blue/yellow robots for role classification
            {
                OwnRobots = repo.InData.Opponent().ToList<SSL_DetectionRobot>();
                OppRobots = repo.InData.Own().ToList<SSL_DetectionRobot>();
            }
            //extract ball(s) info and store in balls for role classification
            List<SSL_DetectionBall> balls = repo.InData.GetBalls().ToList<SSL_DetectionBall>();

            List<float> ratios1 = new List<float>();
            List<float> ratios2 = new List<float>();
            List<float> ratios3 = new List<float>();
            List<float> ratios4 = new List<float>();

            float tmp_Blue = float.PositiveInfinity, tmp_Yellow = float.PositiveInfinity, summation_blue_y = 0, summation_blue_x = 0, summation_yellow_y = 0, summation_yellow_x = 0;
            float tmp1 = 0, tmp2 = 0;
            //defining team sides 
            if (DynamicTeamSideDetection)
            {
                for (int i = 0; i < 6; i++)     //Check for a variable like maxRobots
                {
                    tmp1 += (float)Math.Sqrt(Math.Pow(OwnRobots[i].x - OwnGoal.X, 2) + Math.Pow(OwnRobots[i].y - OwnGoal.Y, 2));
                    tmp2 += (float)Math.Sqrt(Math.Pow(OwnRobots[i].x - OppGoal.X, 2) + Math.Pow(OwnRobots[i].y - OppGoal.Y, 2));
                }
                if (tmp2 < tmp1)
                {
                    OwnGoal.X = 3025;
                    OppGoal.X = -3025;
                }
            }
            //DynamicRoleAssignment
            if (DynamicRoleAssignment)
            {
                float Ratio3, Ratio4, tmp9;
                for (int i = 0; i < 6; i++)
                {
                    //Goal_Keeper
                    Ratio3 = Distance(OwnRobots[i].x, OwnGoal.X, OwnRobots[i].y, OwnGoal.Y) / Distance(OwnRobots[i].x, OppGoal.X, OwnRobots[i].y, OppGoal.Y);
                    ratios3.Add(Ratio3);
                    if (Ratio3 < tmp_Blue)
                    {
                        tmp_Blue = Ratio3;
                        OwnGoalkeeperId = i;
                    }
                    //Goal_Keeper_End
                    //Attacker_Blue and Attack_Support
                    Ratio4 = Distance(OwnRobots[i].x, balls[0].x, OwnRobots[i].y, balls[0].y) / Distance(OwnRobots[i].x, OwnGoal.X, OwnRobots[i].y, OwnGoal.Y);
                    ratios1.Add(Ratio4);
                    //Marker
                    tmp9 = (float)Math.Sqrt(Math.Pow(OwnRobots[i].x - balls[0].x, 2) + Math.Pow(OwnRobots[i].y - balls[0].y, 2));
                    if (tmp9 < OwnClosestRobotDistance)
                    {
                        OwnClosestRobotDistance = tmp9;
                        OwnMarkerId = i;
                    }
                }
                float least_ratio = 1000;
                for (int i = 0; i < 6; i++)
                {
                    if (ratios1[i] < least_ratio)
                    {
                        least_ratio = ratios1[i];
                        OwnAttackerId = i;
                    }
                }
                ratios1[OwnAttackerId] = float.PositiveInfinity;
                least_ratio = 1000;
                for (int i = 0; i < 6; i++)
                {
                    if (ratios1[i] < least_ratio)
                    {
                        least_ratio = ratios1[i];
                        OwnAttackSupportId = i;
                    }
                }
                ratios3[OwnGoalkeeperId] = float.PositiveInfinity;
                least_ratio = 1000;
                for (int i = 0; i < 6; i++)
                {
                    if (ratios3[i] < least_ratio && i != OwnGoalkeeperId && i != OwnAttackerId && i != OwnAttackSupportId)
                    {
                        least_ratio = ratios3[i];
                        OwnDefender1Id = i;
                    }
                }
                ratios3[OwnDefender1Id] = float.PositiveInfinity;
                least_ratio = 1000;
                for (int i = 0; i < 6; i++)
                {
                    if (ratios3[i] < least_ratio && i != OwnGoalkeeperId && i != OwnAttackerId && i != OwnAttackSupportId && i != OwnDefender1Id)
                    {
                        least_ratio = ratios3[i];
                        OwnDefender2Id = i;
                    }
                }
                for (int i = 0; i < 6; i++)
                {
                    if (i != OwnGoalkeeperId && i != OwnDefender1Id && i != OwnDefender2Id && i != OwnAttackSupportId && i != OwnAttackerId)
                    {
                        OwnFreeRoleId = i;
                    }
                }
                for (int i = 0; i < 6; i++)
                {

                    OwnRobotsMean.X += OwnRobots[i].x;
                    OwnRobotsMean.Y += OwnRobots[i].y;
                }

                OwnRobotsMean.X = OwnRobotsMean.X / 6;
                OwnRobotsMean.Y = OwnRobotsMean.Y / 6;

                for (int i = 0; i < 6; i++)
                {
                    summation_blue_y += (float)Math.Pow(OwnRobots[i].y - OwnRobotsMean.Y, 2);
                    summation_blue_x += (float)Math.Pow(OwnRobots[i].x - OwnRobotsMean.X, 2);
                }
                OwnRobotsStandardDeviation.Y = (float)Math.Sqrt(summation_blue_y / 6);
                OwnRobotsStandardDeviation.X = (float)Math.Sqrt(summation_blue_x / 6);
            }
            else
            {
                StaticRoleAssignments();
            }
            float Ratio13, Ratio14;
            float tmp19;
            for (int i = 0; i < 6; i++)
            {
                Ratio13 = Distance(OppRobots[i].x, OppGoal.X, OppRobots[i].y, OppGoal.Y) / Distance(OppRobots[i].x, OwnGoal.X, OppRobots[i].y, OwnGoal.Y);
                ratios4.Add(Ratio13);
                if (Ratio13 < tmp_Yellow)
                {
                    tmp_Yellow = Ratio13;
                    OppGoalkeeperId = i;
                }
                Ratio14 = Distance(OppRobots[i].x, balls[0].x, OppRobots[i].y, balls[0].y) / Distance(OppRobots[i].x, OppGoal.X, OppRobots[i].y, OppGoal.Y);
                ratios2.Add(Ratio14);
                tmp19 = (float)Math.Sqrt(Math.Pow(OppRobots[i].x - balls[0].x, 2) + Math.Pow(OppRobots[i].y - balls[0].y, 2));
                if (tmp19 < OppClosestRobotDistance)
                {
                    OppClosestRobotDistance = tmp19;
                    OppMarkerId = i;
                }
            }
            //Marker
            float least_ratio2 = 1000;
            for (int i = 0; i < 6; i++)
            {
                if (ratios2[i] < least_ratio2)
                {
                    least_ratio2 = ratios2[i];
                    OppAttackerId = i;
                }
            }
            ratios2[OppAttackerId] = float.PositiveInfinity;
            least_ratio2 = 1000;
            for (int i = 0; i < 6; i++)
            {
                if (ratios2[i] < least_ratio2)
                {
                    least_ratio2 = ratios2[i];
                    OppAttackSupportId = i;
                }
            }
            //Defenders
            ratios4[OppGoalkeeperId] = float.PositiveInfinity;
            least_ratio2 = 1000;
            for (int i = 0; i < 6; i++)
            {
                if (ratios4[i] < least_ratio2 && i != OppGoalkeeperId && i != OppAttackerId && i != OppAttackSupportId)
                {
                    least_ratio2 = ratios4[i];
                    OppDefender1Id = i;
                }
            }
            ratios4[OppDefender1Id] = float.PositiveInfinity;
            least_ratio2 = 1000;
            for (int i = 0; i < 6; i++)
            {
                if (ratios4[i] < least_ratio2 && i != OppGoalkeeperId && i != OppAttackerId && i != OppAttackSupportId && i != OppDefender1Id)
                {
                    least_ratio2 = ratios4[i];
                    OppDefender2Id = i;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                if (i != OppGoalkeeperId && i != OppDefender1Id && i != OppDefender2Id && i != OppAttackSupportId && i != OppAttackerId)
                {
                    OppFreeRoleId = i;
                }
            }

            for (int i = 0; i < 6; i++)
            {
                OppRobotsMean.X += OppRobots[i].x;
                OppRobotsMean.Y += OppRobots[i].y;
            }
            OppRobotsMean.X = OppRobotsMean.X / 6;
            OppRobotsMean.Y = OppRobotsMean.Y / 6;

            for (int i = 0; i < 6; i++)
            {
                summation_yellow_y += (float)Math.Pow(OppRobots[i].y - OppRobotsMean.Y, 2);
                summation_yellow_x += (float)Math.Pow(OppRobots[i].x - OppRobotsMean.X, 2);
            }
            OppRobotsStandardDeviation.Y = (float)Math.Sqrt(summation_yellow_y / 6);
            OppRobotsStandardDeviation.X = (float)Math.Sqrt(summation_yellow_x / 6);
            //rebuff = "Team Blue \r\n Free Role=" + Free_Role_Blue.ToString() + " Attacker=" + Attacker_Blue.ToString() + " Support=" + Attack_Supporter_Blue.ToString() + " Defender=" + Defender1_Blue.ToString() + " Defender=" + Defender2_Blue.ToString() + " Keeper=" + GoalKeeper_Blue_id.ToString() + "\r\n";
            //rebuff += "Team Yellow \r\n Free Role=" + Free_Role_Yellow.ToString() + " Attacker=" + Attacker_Yellow.ToString() + " Support=" + Attack_Supporter_Yellow.ToString() + " Defender=" + Defender1_Yellow.ToString() + " Defender=" + Defender2_Yellow.ToString() + " Keeper=" + GoalKeeper_Yellow_id.ToString();
            //MessageBox.Show("musaub");
        }

        public void StaticRoleAssignments()
        {
            OwnGoalkeeperId = 5;
            OwnDefender1Id = 0;
            OwnDefender2Id = 2;
            OwnAttackerId = 3;
            OwnAttackSupportId = 1;
            OwnFreeRoleId = 4;
            //GoalKeeper_Yellow_id = 5;
            //Defender1_Yellow = 0;
            //Defender2_Yellow = 2;
            //Attacker_Yellow = 3;
            //Attack_Supporter_Yellow = 1;
            //Free_Role_Yellow = 4;
        }

        //public void ActionAssignments()   //This is the uncachnged code. before removing the blue and yellow thing.
        //{
        //    if (repo.Configuration.IsBlueTeam)                                     //OwnTeam=Blue
        //    {
        //        if (OwnClosestRobotDistance < OppClosestRobotDistance)    //possesion=Blue
        //        {

        //            BlockOppLineOfSight(OwnGoalkeeperId, OwnDefender1Id, OwnDefender2Id, RobotToIRobotInfo(repo.InData.Own(OwnGoalkeeperId)), RobotToIRobotInfo(repo.InData.Own(OwnDefender1Id)), RobotToIRobotInfo(repo.InData.Own(OwnDefender2Id)), repo.InData.GetBalls(0)[0], new RobotParameters(OwnGoal.X, OwnGoal.Y, 0));   //CMDRAGONS
        //            //bool freespace = MakeFreeSpace(Attack_Supporter_Blue, Free_Role_Blue, RobotToIRobotInfo(repo.InData.Own(Attack_Supporter_Blue)), RobotToIRobotInfo(repo.InData.Own(Free_Role_Blue)), repo.InData.GetBalls()[0], Yellow_Mean, Yellow_SD);     //AttackSp and FreeRole make free Space
        //            //bool point= GotoPoint(Attacker_Blue, RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]), BallToIRobotInfo(packet.detection.balls[0]), 300) ;
        //            //if (freespace & point)
        //            //{
        //            //    int[] arr = { Attacker_Blue, Attack_Supporter_Blue, Free_Role_Blue };
        //            //    if (CheckLineOfSight(RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]), RobotToIRobotInfo(packet.detection.robots_blue[Attack_Supporter_Blue]), AllRobotsExcept(packet, arr)))
        //            RotateArroundPoint(OwnAttackerId, RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), BallToIRobotInfo(repo.InData.GetBalls(0)[0]), RobotToIRobotInfo(repo.InData.Own(OwnGoalkeeperId)), 250);
        //                        //if (GrabBall(Attacker_Blue, RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]), packet.detection.balls[0]))
        //                        // if  (KickBall(Attacker_Blue, RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]), packet.detection.balls[0], 3))
        //                        //        DataHandler.Dribble[Attack_Supporter_Blue]=true;

        //            //}


        //            //            if(RotateArroundPoint(Attacker_Blue,RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]),BallToIRobotInfo(packet.detection.balls[0]),RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]),200))
        //            //                if(GrabBall(Attacker_Blue,RobotToIRobotInfo(packet.detection.robots_blue[Attacker_Blue]),packet.detection.balls[0]));

        //            //    }
        //            //    else
        //            //    {
        //            //        if (CheckLineOfSight(BallToIRobotInfo(packet.detection.balls[0]), RobotToIRobotInfo(packet.detection.robots_blue[Free_Role_Blue]), AllRobotsExcept(packet, arr)))
        //            //        {
        //            //        }
        //            //    }
        //            //}

        //        }
        //    }

        //}
        public void ActionAssignments()
        {
            // Keep defenders and goalkeeper in position.
            BlockOppLineOfSight(OwnGoalkeeperId, OwnDefender1Id, OwnDefender2Id, RobotToIRobotInfo(repo.InData.Own(OwnGoalkeeperId)), RobotToIRobotInfo(repo.InData.Own(OwnDefender1Id)), RobotToIRobotInfo(repo.InData.Own(OwnDefender2Id)), repo.InData.GetBalls(0)[0], new RobotParameters(OwnGoal.X, OwnGoal.Y, 0));   //CMDRAGONS
            // If possesion is with own team.
            if (OwnClosestRobotDistance < OppClosestRobotDistance)
            {
                // Send robots to make free space for pass incase attacker gets blocked.
                bool freespace = MakeFreeSpace(OwnAttackSupportId, OwnFreeRoleId, RobotToIRobotInfo(repo.InData.Own(OwnAttackSupportId)), RobotToIRobotInfo(repo.InData.Own(OwnFreeRoleId)), repo.InData.GetBalls()[0], OppRobotsMean, OppRobotsStandardDeviation);     //AttackSp and FreeRole make free Space
                // Send Attacker to grab ball.
                bool point = GotoPoint(OwnAttackerId, RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), BallToIRobotInfo(repo.InData.GetBalls()[0]), 300);
                if (freespace & point)
                {
                    int[] arr = { OwnAttackerId, OwnAttackSupportId, OwnFreeRoleId };
                    if (true) //GoalInSight(); //btw it should check for direct goal :/
                    {   //Check for Line of Sight with FreeSpaceRobots to pass
                        if (CheckLineOfSight(RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), RobotToIRobotInfo(repo.InData.Own(OwnAttackSupportId)), AllRobotsExcept(arr)))
                        {
                            //If Space Found Rotate
                            if (RotateArroundPoint(OwnAttackerId, RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), BallToIRobotInfo(repo.InData.GetBalls(0)[0]), RobotToIRobotInfo(repo.InData.Own(OwnGoalkeeperId)), 250)) ;       //Returs opposite . i.e. if line of sight is not clear it returs true :/
                            {
                                //Grab ball
                                if (GrabBall(OwnAttackerId, RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), repo.InData.GetBalls()[0]))
                                {
                                    //Pass it to the robot.
                                    if (KickBall(OwnAttackerId, RobotToIRobotInfo(repo.InData.Own(OwnAttackerId)), repo.InData.GetBalls()[0], 3))
                                    {
                                        // set dribbler on for attack Support or whoever is receiving pass.
                                    }
                                }
                            }
                        }
                    }
                    // if line of sight is clear
                    else
                    {
                        //ShootOnGoal
                    }
                }
            }
            // If possesion is opponents
            else
            {
                //Block_Attacker(); // new version should be implemented.
                //Mark_Opp_Attack_Support(); //New version off this should be implemented.
            }

        }
        #endregion

        #region Old
        public void GrabOrKick(SSL_WrapperPacket packet, int id)
        {
            if (repo.Configuration.IsBlueTeam)
            {
                if (Distance(packet.detection.robots_blue[id].x, packet.detection.balls[0].x, packet.detection.robots_blue[id].y, packet.detection.balls[0].y) > 105)
                    GrabBall(packet, repo.Configuration.IsBlueTeam);
                else
                    Kickball(packet, id, 5, repo.Configuration.IsBlueTeam);
            }
            else
            {
                if (Distance(packet.detection.robots_yellow[id].x, packet.detection.balls[0].x, packet.detection.robots_yellow[id].y, packet.detection.balls[0].y) > 105)
                    GrabBall(packet, repo.Configuration.IsBlueTeam);
                else
                    Kickball(packet, id, 5, repo.Configuration.IsBlueTeam);
            }
        }

        public void rotateabout(SSL_WrapperPacket packet, IRobotInfo target, float conf)
        {
            //packet.detection.robots_blue[Attacker_Blue]
            //PointF[] paray=new PointF[360];
            //for (int i=0;i<360;i++)
            float finalangle = (float)Math.Atan2(packet.detection.balls[0].y - target.Y, packet.detection.balls[0].x - target.X);
            IRobotInfo rb1 = new RobotParameters();
            float i = (float)Math.Atan2(packet.detection.robots_blue[OwnAttackerId].y - packet.detection.balls[0].y, packet.detection.robots_blue[OwnAttackerId].x - packet.detection.balls[0].x);
            float Offset = 1.30f;//0.69808027923211169284467713787086f;
            if (Math.Abs(finalangle - i) > Math.PI)
                Offset = -Offset;
            if (finalangle - i > Offset)//0.69808027923211169284467713787086f)
            {
                //rebuff = (finalangle * 57.3).ToString() + " x " + (i * 57.3).ToString();
                i += Offset;
                rb1.X = packet.detection.balls[0].x + conf * (float)Math.Cos(i);
                rb1.Y = packet.detection.balls[0].y + conf * (float)Math.Sin(i);
                //rb1.w = (float)Math.Atan2(packet.detection.balls[0].y - packet.detection.robots_blue[Attacker_Blue].y, packet.detection.balls[0].x - packet.detection.robots_blue[Attacker_Blue].x);
                //rb1.w += (float)Offset;
                repo.OutData[OwnAttackerId].X = rb1.X;
                repo.OutData[OwnAttackerId].Y = rb1.Y;
                repo.OutData[OwnAttackerId].W = (float)Math.Atan2(0 - repo.InData.GetBalls(0)[0].y, 3025 - repo.InData.GetBalls(0)[0].x);
                //DataHandler.WriteReference(new RobotParameters(rb1.X, rb1.Y, (float)Math.Atan2(0 - packet.detection.balls[0].y, 3025 - packet.detection.balls[0].x)), Attacker_Blue);
            }
            else if (finalangle - i > 0.05f)
            {
                //rebuff = (finalangle * 57.3).ToString() + Environment.NewLine;
                rb1.X = packet.detection.balls[0].x + conf * (float)Math.Cos(finalangle);
                rb1.Y = packet.detection.balls[0].y + conf * (float)Math.Sin(finalangle);
                //rb1.w = (float)Math.Atan2(packet.detection.balls[0].y - packet.detection.robots_blue[Attacker_Blue].y, packet.detection.balls[0].x - packet.detection.robots_blue[Attacker_Blue].x);
                //rb1.w += (float)(40 / 57.3);
                repo.OutData[OwnAttackerId].X = rb1.X;
                repo.OutData[OwnAttackerId].Y = rb1.Y;
                repo.OutData[OwnAttackerId].X = (float)Math.Atan2(0 - packet.detection.balls[0].y, 3025 - packet.detection.balls[0].x);
                //DataHandler.WriteReference(new IRobotInfo(rb1.X, rb1.Y, (float)Math.Atan2(0 - packet.detection.balls[0].y, 3025 - packet.detection.balls[0].x)), Attacker_Blue);

            }
            else
                GrabOrKick(packet, OwnAttackerId);

        }

        //public void ActionAssignments(SSL_WrapperPacket packet)
        //{

        //    if (repo.Configuration.IsBlueTeam)
        //    {

        //        if (Closest_Blue_Distance < Closest_Yellow_Distance)
        //        {
        //            MakeFreeSpace(packet, repo.Configuration.IsBlueTeam);
        //            Block_Line_of_Sight(packet, repo.Configuration.IsBlueTeam);
        //            if (false)//GoalInSight(packet, repo.Configuration.IsBlueTeam))
        //            {
        //                GoNearandRotate(packet, true, 300, new IRobotInfo(3025, 0, 0));
        //            }
        //            else
        //            {
        //                CheckandPass(packet, repo.Configuration.IsBlueTeam);
        //            }
        //        }
        //        else
        //        {
        //            Block_Attacker(packet, repo.Configuration.IsBlueTeam);
        //            //if (Goal_in_Sight(ref packet, 2))
        //            //{
        //            //    Shoot_at_Goal(2);
        //            //}
        //            //else
        //            //{
        //            //    Pass(2);
        //            //}
        //            Mark_Opp_Attack_Support(packet, repo.Configuration.IsBlueTeam);
        //            Block_Line_of_Sight(packet, repo.Configuration.IsBlueTeam);

        //        }
        //    }
        //    else
        //    {
        //        if (Closest_Blue_Distance < Closest_Yellow_Distance)
        //        {
        //            Block_Attacker(packet, repo.Configuration.IsBlueTeam);
        //            GoalInSight(packet, repo.Configuration.IsBlueTeam);
        //            //{
        //            //    temp(packet, Attacker_Blue);
        //            //}
        //            //else
        //            //{
        //            //    Pass(1);
        //            //}
        //            Mark_Opp_Attack_Support(packet, repo.Configuration.IsBlueTeam);
        //            Block_Line_of_Sight(packet, repo.Configuration.IsBlueTeam);
        //        }
        //        else
        //        {
        //            MakeFreeSpace(packet, repo.Configuration.IsBlueTeam);
        //            Block_Line_of_Sight(packet, repo.Configuration.IsBlueTeam);
        //            if (true)
        //            {
        //                GoNearandRotate(packet, true, 300, new IRobotInfo(-3025, 0, 0));
        //            }
        //            //else
        //            //{
        //            //    Pass(2);
        //            //}
        //        }
        //    }
        //}

        public bool GoalInSight(SSL_WrapperPacket packet, bool IsBlue)
        {
            float slope1, slope2, tmp_slope, width = 350;
            PointF tmp_Point = new PointF(0, 0);
            List<float> slopes = new List<float>();
            List<float> slopes2 = new List<float>();
            List<int> IDs = new List<int>();
            List<PointF> Points = new List<PointF>();
            List<PointF> Final_Points = new List<PointF>();
            if (IsBlue)
            {
                slope1 = (OppGoal.Y + width - packet.detection.robots_blue[OwnAttackerId].y) / (OppGoal.X - packet.detection.robots_blue[OwnAttackerId].x);
                slope2 = (OppGoal.Y - width - packet.detection.robots_blue[OwnAttackerId].y) / (OppGoal.X - packet.detection.robots_blue[OwnAttackerId].x);
                for (int i = 0; i < 6; i++)
                {
                    if (packet.detection.robots_blue[OwnAttackerId].x - packet.detection.robots_yellow[i].x > 0)
                    {
                        tmp_slope = (packet.detection.robots_blue[OwnAttackerId].y - packet.detection.robots_yellow[i].y) / (packet.detection.robots_blue[OwnAttackerId].x - packet.detection.robots_yellow[i].x);
                        slopes.Add(tmp_slope);
                        IDs.Add(i);
                    }
                }
                slopes.Sort();
                //MessageBox.Show("**");
                for (int i = 0; i < slopes.Count; i++)
                {
                    //MessageBox.Show(i.ToString());
                    //MessageBox.Show(slopes.Count.ToString());
                    if (slopes.Count == 0)
                        break;
                    tmp_slope = -1 / slopes[i];
                    if (float.IsInfinity(tmp_slope))
                        tmp_slope = 71;

                    double ratio = 0.1666666;
                    float x = 0, y = 0;
                    while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 70)
                    {
                        if (Math.Abs(tmp_slope) > 8)
                        {
                            x = (float)(x + ratio);
                            y = (float)(y + ratio * tmp_slope);
                        }
                        else
                        {

                            x += 1;
                            y = y + tmp_slope;
                        }
                    }
                    tmp_Point.X = packet.detection.robots_yellow[IDs[i]].x + x;
                    tmp_Point.Y = packet.detection.robots_yellow[IDs[i]].y + y;
                    Points.Add(tmp_Point);

                    x = 0;
                    y = 0;
                    while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 70)
                    {
                        if (Math.Abs(tmp_slope) > 8)
                        {
                            x = (float)(x - ratio);
                            y = (float)(y - ratio * tmp_slope);
                        }
                        else
                        {
                            x -= 1;
                            y = y - tmp_slope;
                        }
                    }
                    tmp_Point.X = packet.detection.robots_yellow[IDs[i]].x + x;
                    tmp_Point.Y = packet.detection.robots_yellow[IDs[i]].y + y;
                    Points.Add(tmp_Point);
                }
                for (int i = 0; i < Points.Count; i += 2)
                {
                    if (Points.Count == 0)
                        break;
                    if (Points[i].Y < Points[i + 1].Y)
                    {
                        tmp_Point = Points[i + 1];
                        Points[i + 1] = Points[i];
                        Points[i] = tmp_Point;
                    }
                }
                slopes.Clear();
                for (int i = 0; i < Points.Count; i++)
                {
                    if (Points.Count == 0)
                        break;
                    tmp_slope = (packet.detection.robots_blue[OwnAttackerId].y - Points[i].Y) / (packet.detection.robots_blue[OwnAttackerId].x - Points[i].X);
                    slopes.Add(tmp_slope);
                }
                Points.Clear();
                for (int i = 0; i < slopes.Count; i += 2)
                {
                    tmp_Point.X = OppGoal.X;
                    tmp_Point.Y = slopes[i] * (OppGoal.X - packet.detection.robots_blue[OwnAttackerId].x) + packet.detection.robots_blue[OwnAttackerId].y;
                    Points.Add(tmp_Point);
                    tmp_Point.X = OppGoal.X;
                    tmp_Point.Y = slopes[i + 1] * (OppGoal.X - packet.detection.robots_blue[OwnAttackerId].x) + packet.detection.robots_blue[OwnAttackerId].y;
                    Points.Add(tmp_Point);
                }
                //MessageBox.Show("**");
                for (int i = 0; i < Points.Count; i++)
                {
                    //MessageBox.Show(Points[i].X.ToString() + " " + Points[i].Y.ToString() + " " + i.ToString());
                    tmp_Point.X = 3025;
                    if (Points[i].Y < (OppGoal.Y - 350))
                    {
                        tmp_Point.Y = -350;
                        Points[i] = tmp_Point;
                    }
                    if (Points[i].Y > (OppGoal.Y + 350))
                    {
                        tmp_Point.Y = 350;
                        Points[i] = tmp_Point;
                    }
                }

                for (int i = 0; i < Points.Count; i += 2)
                {
                    if ((Points[i].Y > 350 && Points[i + 1].Y > 350) || (Points[i].Y < -350 && Points[i + 1].Y < -350))
                    {
                        Points.RemoveAt(i);
                        Points.RemoveAt(i + 1);
                    }
                }

                for (int i = 0; i < Points.Count; i++)
                {
                    for (int j = 0; j < Points.Count; j += 2)
                    {
                        if (Points[i].Y < Points[j].Y && Points[i].Y > Points[j + 1].Y)
                        {
                            if (i % 2 == 0)
                            {
                                tmp_Point.Y = Points[j].Y;
                                Points[i] = tmp_Point;
                                if (Points[i + 1].Y > Points[j + 1].Y)
                                {
                                    tmp_Point.Y = Points[j + 1].Y;
                                    Points[i + 1] = tmp_Point;
                                }
                                else
                                {
                                    tmp_Point.Y = Points[i + 1].Y;
                                    Points[j + 1] = tmp_Point;
                                }
                            }
                            else
                            {
                                tmp_Point.Y = Points[j + 1].Y;
                                Points[i] = tmp_Point;
                                if (Points[i - 1].Y < Points[j].Y)
                                {
                                    tmp_Point.Y = Points[j].Y;
                                    Points[i - 1] = tmp_Point;
                                }
                                else
                                {
                                    tmp_Point.Y = Points[i - 1].Y;
                                    Points[j] = tmp_Point;
                                }
                            }
                        }
                    }
                }
                for (int i = 0; i < Points.Count; i++)
                {
                    tmp_Point.Y = 0;
                    if (i == 0)
                    {
                        Final_Points.Add(Points[i]);
                    }
                    for (int j = 0; j < Final_Points.Count; j++)
                    {
                        if (Points[i] == Final_Points[j])
                        {
                            tmp_Point.Y = 1;
                        }
                    }
                    if (tmp_Point.Y == 0)
                    {
                        Final_Points.Add(Points[i]);
                    }
                }
                float max_dist = 0;
                float tmp_dist = 0;
                for (int i = 0; i < Final_Points.Count; i++)
                {
                    if (i == 0)
                    {
                        tmp_dist = OppGoal.Y + 350 - Final_Points[i].Y;
                        max_dist = tmp_dist;
                        tmp_Point.Y = (OppGoal.Y + 350 + Final_Points[i].Y) / 2;
                    }
                    else
                    {
                        if (i == Final_Points.Count - 1)
                        {
                            tmp_dist = Math.Abs(OppGoal.Y - 350 - Final_Points[i].Y);
                            if (tmp_dist > max_dist)
                            {
                                max_dist = tmp_dist;
                                tmp_Point.Y = (OppGoal.Y - 350 + Final_Points[i].Y) / 2;
                            }
                        }
                        else
                        {
                            if (i % 2 == 0)
                            {
                                continue;
                            }
                            else
                            {
                                tmp_dist = Math.Abs(Final_Points[i].Y - Final_Points[i + 1].Y);
                                if (tmp_dist > max_dist)
                                {
                                    max_dist = tmp_dist;
                                    tmp_Point.Y = (Final_Points[i].Y + Final_Points[i + 1].Y) / 2;
                                }
                            }
                        }
                    }
                }
                //MessageBox.Show(tmp_Point.X.ToString() + " " + tmp_Point.Y.ToString());
                if (max_dist >= 50)
                {
                    ShootingAngle = (float)Math.Atan2(packet.detection.robots_blue[OwnAttackerId].y - tmp_Point.Y, packet.detection.robots_blue[OwnAttackerId].x - 0);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                return true;
            }
        }

        public void GrabBall(SSL_WrapperPacket packet, bool IsBlue)
        {
            if (IsBlue)
            {
                IRobotInfo final = new RobotParameters(packet.detection.balls[0].x, packet.detection.balls[0].y,
                    (float)Math.Atan2(packet.detection.balls[0].y - packet.detection.robots_blue[OwnAttackerId].y,
                    packet.detection.balls[0].x - packet.detection.robots_blue[OwnAttackerId].x));
                repo.OutData[OwnAttackerId].X = final.X;
                repo.OutData[OwnAttackerId].X = final.Y;
                repo.OutData[OwnAttackerId].X = final.W;
                //DataHandler.WriteReference(final, Attacker_Blue);
                repo.OutData[OwnAttackerId].Grab = true;
                //DataHandler.Dribble[Attacker_Blue] = true;
            }
            else
            {
                IRobotInfo final = new RobotParameters(packet.detection.balls[0].x, packet.detection.balls[0].y,
                    (float)Math.Atan2(packet.detection.balls[0].y - packet.detection.robots_yellow[OppAttackerId].y,
                    packet.detection.balls[0].x - packet.detection.robots_yellow[OppAttackerId].x));
                repo.OutData[OppAttackerId].X = final.X;
                repo.OutData[OppAttackerId].X = final.Y;
                repo.OutData[OppAttackerId].X = final.W;
                //DataHandler.WriteReference(final, Attacker_Yellow);
                repo.OutData[OppAttackerId].Grab = true;
                //DataHandler.Dribble[Attacker_Yellow] = true;
            }

        }

        public void GoNearBall(SSL_WrapperPacket packet, bool IsBlue, float conf)
        {
            if (IsBlue)
            {
                float dis = Distance(packet.detection.robots_blue[OwnAttackerId].x, packet.detection.balls[0].x, packet.detection.robots_blue[OwnAttackerId].y, packet.detection.balls[0].y);
                float angle = (float)Math.Atan2(packet.detection.balls[0].y - packet.detection.robots_blue[OwnAttackerId].y, packet.detection.balls[0].x - packet.detection.robots_blue[OwnAttackerId].x);
                dis -= conf;
                IRobotInfo Target = new RobotParameters(packet.detection.robots_blue[OwnAttackerId].x + (float)(dis * Math.Cos(angle)), packet.detection.robots_blue[OwnAttackerId].y + (float)(dis * Math.Sin(angle)), (float)Math.Atan2(0 - packet.detection.balls[0].y, 3025 - packet.detection.balls[0].x));
                repo.OutData[OwnAttackerId].X = Target.X;
                repo.OutData[OwnAttackerId].Y = Target.Y;
                repo.OutData[OwnAttackerId].W = Target.W;
                //DataHandler.WriteReference(Target, Attacker_Blue);
            }
        }

        public void GoNearandRotate(SSL_WrapperPacket packet, bool IsBlue, float conf, IRobotInfo target)
        {
            if (Distance(packet.detection.robots_blue[OwnAttackerId].x, packet.detection.balls[0].x, packet.detection.robots_blue[OwnAttackerId].y, packet.detection.balls[0].y) > conf)
                GoNearBall(packet, true, conf - 200);
            else
            {
                rotateabout(packet, target, 350);

            }

        }

        public void Kickball(SSL_WrapperPacket packet, int id, float speed, bool IsBlue)
        {
            if (IsBlue)
            {
                repo.OutData[id].KickSpeed = speed;
                //DataHandler.WriteKickSpeed(speed, id);
                System.Threading.Thread.Sleep(100);
                repo.OutData[id].KickSpeed = 0;
                //DataHandler.WriteKickSpeed(0, id);
                repo.OutData[id].Grab = false;
                //DataHandler.Dribble[id] = false;
            }
            else
            {
                repo.OutData[id].KickSpeed = speed;
                //DataHandler.WriteKickSpeed(speed, id);
                System.Threading.Thread.Sleep(100);
                repo.OutData[id].KickSpeed = 0;
                //DataHandler.WriteKickSpeed(0, id);
                repo.OutData[id].Grab = false;
                //DataHandler.Dribble[id] = false;
            }
        }

        public void Block_Attacker(SSL_WrapperPacket packet, bool IsBlue)
        {
            IRobotInfo Final_Point_Marker = new RobotParameters(0, 0, 0);
            float Final_Angle = 0;
            float tmp_slope = 0;
            if (IsBlue)
            {
                tmp_slope = (OwnGoal.Y - packet.detection.balls[0].y) / (OwnGoal.X - packet.detection.balls[0].x);
                if (float.IsInfinity(tmp_slope))
                    tmp_slope = 300;
                float x = 0;
                float y = 0;
                double ratio = 0.1666666666;
                while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 300)
                {
                    if (tmp_slope > 9)
                    {
                        x = (float)(x - ratio);
                        y = (float)(y - ratio * tmp_slope);
                    }
                    else
                    {
                        x = x - 1;
                        y = y - tmp_slope;
                    }
                }
                Final_Point_Marker.X = packet.detection.balls[0].x + x;
                Final_Point_Marker.Y = packet.detection.balls[0].y + y;
                Final_Angle = (float)Math.Atan2(OwnGoal.Y - packet.detection.balls[0].y, OwnGoal.X - packet.detection.balls[0].x);
                Final_Angle = (float)(Final_Angle + Math.PI);
                Final_Point_Marker.W = Final_Angle;

                repo.OutData[OwnAttackerId].X = Final_Point_Marker.X;
                repo.OutData[OwnAttackerId].Y = Final_Point_Marker.Y;
                repo.OutData[OwnAttackerId].W = Final_Point_Marker.W;
                //DataHandler.WriteReference(Final_Point_Marker, Attacker_Blue);
            }
            else
            {
                tmp_slope = (OppGoal.Y - packet.detection.balls[0].y) / (OppGoal.X - packet.detection.balls[0].x);
                if (float.IsInfinity(tmp_slope))
                    tmp_slope = 300;
                float x = 0;
                float y = 0;
                double ratio = 0.16666666666;
                while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 300)
                {
                    if (tmp_slope > 9)
                    {
                        x = (float)(x + ratio);
                        y = (float)(y + ratio * tmp_slope);
                    }
                    else
                    {
                        x = x + 1;
                        y = y + tmp_slope;
                    }
                }
                Final_Point_Marker.X = packet.detection.balls[0].x + x;
                Final_Point_Marker.Y = packet.detection.balls[0].y + y;
                Final_Angle = (float)Math.Atan2(OppGoal.Y - packet.detection.balls[0].y, OppGoal.X - packet.detection.balls[0].x);
                Final_Angle = (float)(Final_Angle + Math.PI);
                Final_Point_Marker.W = Final_Angle;

                repo.OutData[OppAttackerId].X = Final_Point_Marker.X;
                repo.OutData[OppAttackerId].Y = Final_Point_Marker.Y;
                repo.OutData[OppAttackerId].W = Final_Point_Marker.W;
                //DataHandler.WriteReference(Final_Point_Marker, Attacker_Yellow);
                //Yellow Function Goes Here
            }
            //Use Marker_Yellow for team 2 and Marker_Blue for team 1.            
        }

        public void Block_Line_of_Sight(SSL_WrapperPacket packet, bool IsBlue)
        {
            if (IsBlue)
            {
                if (packet.detection.balls[0].x > -3025)
                {
                    List<float> slopes = new List<float>();
                    IRobotInfo[] final_points = new IRobotInfo[3];
                    List<IRobotInfo> points_goal = new List<IRobotInfo>();
                    List<float> final_angles = new List<float>();
                    int point_with_small_y = 0, point_with_big_y = 2;
                    IRobotInfo tmp_point = new RobotParameters(-3025, 0, 0);
                    tmp_point.Y = -232;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    tmp_point.Y = 0;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    tmp_point.Y = 232;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    slopes.Add((OwnGoal.Y - 232 - packet.detection.balls[0].y) / (OwnGoal.X - packet.detection.balls[0].x));
                    slopes.Add((OwnGoal.Y + 0 - packet.detection.balls[0].y) / (OwnGoal.X - packet.detection.balls[0].x));
                    slopes.Add((OwnGoal.Y + 232 - packet.detection.balls[0].y) / (OwnGoal.X - packet.detection.balls[0].x));
                    float x = 0, y = 0;
                    double ratio = 0.1666666666;
                    for (int i = 0; i < 3; i++)
                    {
                        if (float.IsInfinity(slopes[i]))
                            slopes[i] = 300;

                        if (i == 1)
                        {
                            while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 375)
                            {
                                if (slopes[i] > 9)
                                {
                                    x = (float)(x + ratio);
                                    y = (float)(y + ratio * slopes[i]);
                                }
                                else
                                {
                                    x = x + 1;
                                    y = y + slopes[i];
                                }
                            }
                        }
                        else
                        {
                            while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 800)
                            {
                                if (slopes[i] > 9)
                                {
                                    x = (float)(x + ratio);
                                    y = (float)(y + ratio * slopes[i]);
                                }
                                else
                                {
                                    x = x + 1;
                                    y = y + slopes[i];
                                }
                            }
                        }

                        tmp_point.X = points_goal[i].X + x;
                        tmp_point.Y = points_goal[i].Y + y;
                        final_points[i] = Common.Statics.DeepClone<IRobotInfo>(tmp_point);
                        x = 0;
                        y = 0;
                    }


                    final_angles.Add((float)Math.Atan2(OwnGoal.Y - 232 - packet.detection.balls[0].y, OwnGoal.X - packet.detection.balls[0].x));
                    final_angles.Add((float)Math.Atan2(OwnGoal.Y - 0 - packet.detection.balls[0].y, OwnGoal.X - packet.detection.balls[0].x));
                    final_angles.Add((float)Math.Atan2(OwnGoal.Y + 232 - packet.detection.balls[0].y, OwnGoal.X - packet.detection.balls[0].x));


                    for (int i = 0; i < 3; i++)
                    {
                        final_angles[i] += (float)Math.PI;
                        final_points[i].W = final_angles[i];
                    }

                    if (final_points[2].Y < final_points[0].Y)
                    {
                        point_with_small_y = 2;
                        point_with_big_y = 0;
                    }

                    if (packet.detection.robots_blue[OwnDefender1Id].y < packet.detection.robots_blue[OwnDefender2Id].y)
                    {
                        repo.OutData[OwnDefender1Id].X = final_points[point_with_small_y].X;
                        repo.OutData[OwnDefender1Id].Y = final_points[point_with_small_y].Y;
                        repo.OutData[OwnDefender1Id].W = final_points[point_with_small_y].W;
                        //DataHandler.WriteReference(final_points[point_with_small_y], Defender1_Blue);
                        repo.OutData[OwnGoalkeeperId].X = final_points[1].X;
                        repo.OutData[OwnGoalkeeperId].Y = final_points[1].Y;
                        repo.OutData[OwnGoalkeeperId].W = final_points[1].W;
                        //DataHandler.WriteReference(final_points[1], GoalKeeper_Blue_id);
                        repo.OutData[OwnDefender2Id].X = final_points[point_with_big_y].X;
                        repo.OutData[OwnDefender2Id].Y = final_points[point_with_big_y].Y;
                        repo.OutData[OwnDefender2Id].W = final_points[point_with_big_y].W;
                        //DataHandler.WriteReference(final_points[point_with_big_y], Defender2_Blue);
                    }
                    else
                    {
                        repo.OutData[OwnDefender1Id].X = final_points[point_with_big_y].X;
                        repo.OutData[OwnDefender1Id].Y = final_points[point_with_big_y].Y;
                        repo.OutData[OwnDefender1Id].W = final_points[point_with_big_y].W;
                        //DataHandler.WriteReference(final_points[point_with_big_y], Defender1_Blue);
                        repo.OutData[OwnGoalkeeperId].X = final_points[1].X;
                        repo.OutData[OwnGoalkeeperId].Y = final_points[1].Y;
                        repo.OutData[OwnGoalkeeperId].W = final_points[1].W;
                        //DataHandler.WriteReference(final_points[1], GoalKeeper_Blue_id);
                        repo.OutData[OwnDefender2Id].X = final_points[point_with_small_y].X;
                        repo.OutData[OwnDefender2Id].Y = final_points[point_with_small_y].Y;
                        repo.OutData[OwnDefender2Id].W = final_points[point_with_small_y].W;
                        //DataHandler.WriteReference(final_points[point_with_small_y], Defender2_Blue);
                    }


                    //if(Distance(packet.detection.robots_blue[Defender1_Blue].x,final_points[0].x,packet.detection.robots_blue[Defender1_Blue].y,final_points[0].y) < Distance(packet.detection.robots_blue[Defender2_Blue].x,final_points[0].x,




                    //rebuff = Defender1_Blue.ToString() + Environment.NewLine + final_points[0].x.ToString() + " " + final_points[0].y.ToString() + " " + final_points[0].w.ToString() +
                    //    Environment.NewLine + GoalKeeper_Blue_id.ToString() + Environment.NewLine + final_points[1].x.ToString() + " " + final_points[1].y.ToString() + " " + final_points[1].w.ToString() +
                    //    Environment.NewLine + Defender2_Blue.ToString() + Environment.NewLine + final_points[2].x.ToString() + " " + final_points[2].y.ToString() + " " + final_points[2].w.ToString();

                }
            }
            else
            {
                if (packet.detection.balls[0].x < 3025)
                {
                    List<float> slopes = new List<float>();
                    IRobotInfo[] final_points = new IRobotInfo[3];
                    List<IRobotInfo> points_goal = new List<IRobotInfo>();
                    List<float> final_angles = new List<float>();
                    int point_with_small_y = 0, point_with_big_y = 2;
                    IRobotInfo tmp_point = new RobotParameters(3025, 0, 0);
                    tmp_point.Y = -232;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    tmp_point.Y = 0;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    tmp_point.Y = 232;
                    points_goal.Add(Statics.DeepClone<IRobotInfo>(tmp_point));
                    slopes.Add((OppGoal.Y - 232 - packet.detection.balls[0].y) / (OppGoal.X - packet.detection.balls[0].x));
                    slopes.Add((OppGoal.Y + 0 - packet.detection.balls[0].y) / (OppGoal.X - packet.detection.balls[0].x));
                    slopes.Add((OppGoal.Y + 232 - packet.detection.balls[0].y) / (OppGoal.X - packet.detection.balls[0].x));
                    float x = 0, y = 0;
                    double ratio = 0.1666666666;
                    for (int i = 0; i < 3; i++)
                    {
                        if (float.IsInfinity(slopes[i]))
                            slopes[i] = 300;
                        if (i == 1)
                        {
                            while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 375)
                            {
                                if (slopes[i] > 9)
                                {
                                    x = (float)(x - ratio);
                                    y = (float)(y - ratio * slopes[i]);
                                }
                                else
                                {
                                    x = x - 1;
                                    y = y - slopes[i];
                                }
                            }
                        }
                        else
                        {
                            while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 800)
                            {
                                if (slopes[i] > 9)
                                {
                                    x = (float)(x - ratio);
                                    y = (float)(y - ratio * slopes[i]);
                                }
                                else
                                {
                                    x = x - 1;
                                    y = y - slopes[i];
                                }
                            }
                        }

                        tmp_point.X = points_goal[i].X + x;
                        tmp_point.Y = points_goal[i].Y + y;
                        final_points[i] = Statics.DeepClone<IRobotInfo>(tmp_point);
                        x = 0;
                        y = 0;

                    }


                    final_angles.Add((float)Math.Atan2(OppGoal.Y - 232 - packet.detection.balls[0].y, OppGoal.X - packet.detection.balls[0].x));
                    final_angles.Add((float)Math.Atan2(OppGoal.Y - 0 - packet.detection.balls[0].y, OppGoal.X - packet.detection.balls[0].x));
                    final_angles.Add((float)Math.Atan2(OppGoal.Y + 232 - packet.detection.balls[0].y, OppGoal.X - packet.detection.balls[0].x));


                    for (int i = 0; i < 3; i++)
                    {
                        final_angles[i] += (float)Math.PI;
                        final_points[i].W = final_angles[i];
                    }

                    if (final_points[2].Y < final_points[0].Y)
                    {
                        point_with_small_y = 2;
                        point_with_big_y = 0;
                    }

                    if (packet.detection.robots_yellow[OppDefender1Id].y < packet.detection.robots_yellow[OppDefender2Id].y)
                    {
                        repo.OutData[OppDefender1Id].X = final_points[point_with_small_y].X;
                        repo.OutData[OppDefender1Id].Y = final_points[point_with_small_y].Y;
                        repo.OutData[OppDefender1Id].W = final_points[point_with_small_y].W;
                        //DataHandler.WriteReference(final_points[point_with_small_y], Defender1_Yellow);
                        repo.OutData[OppGoalkeeperId].X = final_points[1].X;
                        repo.OutData[OppGoalkeeperId].Y = final_points[1].Y;
                        repo.OutData[OppGoalkeeperId].W = final_points[1].W;
                        //DataHandler.WriteReference(final_points[1], GoalKeeper_Yellow_id);
                        repo.OutData[OppDefender2Id].X = final_points[point_with_big_y].X;
                        repo.OutData[OppDefender2Id].Y = final_points[point_with_big_y].Y;
                        repo.OutData[OppDefender2Id].W = final_points[point_with_big_y].W;
                        //DataHandler.WriteReference(final_points[point_with_big_y], Defender2_Yellow);
                    }
                    else
                    {
                        repo.OutData[OppDefender1Id].X = final_points[point_with_big_y].X;
                        repo.OutData[OppDefender1Id].Y = final_points[point_with_big_y].Y;
                        repo.OutData[OppDefender1Id].W = final_points[point_with_big_y].W;
                        //DataHandler.WriteReference(final_points[point_with_big_y], Defender1_Yellow);
                        repo.OutData[OppGoalkeeperId].X = final_points[1].X;
                        repo.OutData[OppGoalkeeperId].Y = final_points[1].Y;
                        repo.OutData[OppGoalkeeperId].W = final_points[1].W;
                        //DataHandler.WriteReference(final_points[1], GoalKeeper_Yellow_id);
                        repo.OutData[OppDefender2Id].X = final_points[point_with_small_y].X;
                        repo.OutData[OppDefender2Id].Y = final_points[point_with_small_y].Y;
                        repo.OutData[OppDefender2Id].W = final_points[point_with_small_y].W;
                        //DataHandler.WriteReference(final_points[point_with_small_y], Defender2_Yellow);
                    }
                }
                //tb_private.Text += "Yellow Defender1 ID " + Defender1_Yellow + " and " + "Yellow Defender2 ID " + Defender2_Yellow + " blocking the goal " + "\r\n";
            }
            //Use Defender1_Yellow and Defender2_Support_Yellow for team 2 and Defender1_Blue and Defender2_Support_Blue for team 1.
        }

        public void MakeFreeSpace(SSL_WrapperPacket packet, bool IsBlue)
        {
            IRobotInfo Final_Point_Support = new RobotParameters();
            IRobotInfo Final_Point_Mid = new RobotParameters();
            if (IsBlue)
            {
                //tb_private.Text += "Blue team's Attack Support ID " + Attack_Supporter_Blue + " and Midfielder ID " + Free_Role_Blue + " move into the free space" + "\r\n";
                if (OppRobotsMean.Y > 0)
                {
                    if (Distance(packet.detection.robots_blue[OwnAttackSupportId].x, OppRobotsMean.X, packet.detection.robots_blue[OwnAttackSupportId].y, OppRobotsMean.Y) < Distance(packet.detection.robots_blue[OwnFreeRoleId].x, OppRobotsMean.X, packet.detection.robots_blue[OwnFreeRoleId].y, OppRobotsMean.Y))
                    {
                        Final_Point_Support.X = OppRobotsMean.X;
                        Final_Point_Support.Y = OppRobotsMean.Y;
                        Final_Point_Mid.X = OppRobotsMean.X;
                        Final_Point_Mid.Y = -9 * OppRobotsStandardDeviation.Y / 5;
                    }
                    else
                    {
                        Final_Point_Support.X = OppRobotsMean.X;
                        Final_Point_Support.Y = -9 * OppRobotsStandardDeviation.Y / 5;
                        Final_Point_Mid.X = OppRobotsMean.X;
                        Final_Point_Mid.Y = OppRobotsMean.Y;
                    }
                }
                else
                {
                    if (Distance(packet.detection.robots_blue[OwnAttackSupportId].x, OppRobotsMean.X, packet.detection.robots_blue[OwnAttackSupportId].y, -OppRobotsMean.Y) < Distance(packet.detection.robots_blue[OwnFreeRoleId].x, OppRobotsMean.X, packet.detection.robots_blue[OwnFreeRoleId].y, -OppRobotsMean.Y))
                    {
                        Final_Point_Support.X = OppRobotsMean.X;
                        Final_Point_Support.Y = -OppRobotsMean.Y;
                        Final_Point_Mid.X = OppRobotsMean.X;
                        Final_Point_Mid.Y = 9 * OppRobotsStandardDeviation.Y / 5;
                    }
                    else
                    {
                        Final_Point_Support.X = OppRobotsMean.X;
                        Final_Point_Support.Y = 9 * OppRobotsStandardDeviation.Y / 5;
                        Final_Point_Mid.X = OppRobotsMean.X;
                        Final_Point_Mid.Y = -OppRobotsMean.Y;
                    }
                }
                Final_Point_Support.W = (float)Math.Atan2(packet.detection.balls[0].y - Final_Point_Support.Y, packet.detection.balls[0].x - Final_Point_Support.X);
                Final_Point_Mid.W = (float)Math.Atan2(packet.detection.balls[0].y - Final_Point_Mid.Y, packet.detection.balls[0].x - Final_Point_Mid.X);

                repo.OutData[OwnAttackSupportId].X = Final_Point_Support.X;
                repo.OutData[OwnAttackSupportId].Y = Final_Point_Support.Y;
                repo.OutData[OwnAttackSupportId].W = Final_Point_Support.W;
                //DataHandler.WriteReference(Final_Point_Support, Attack_Supporter_Blue);
                repo.OutData[OwnFreeRoleId].X = Final_Point_Mid.X;
                repo.OutData[OwnFreeRoleId].Y = Final_Point_Mid.Y;
                repo.OutData[OwnFreeRoleId].W = Final_Point_Mid.W;
                //DataHandler.WriteReference(Final_Point_Mid, Free_Role_Blue);

                //tb_private.Text += "Blue team's Attack Support ID " + Attack_Supporter_Blue + " moves to (" + Final_Point_Support.X + ", " + Final_Point_Support.Y + ") and Midfielder ID " + Free_Role_Blue + " moves to (" + Final_Point_Mid.X + ", " + Final_Point_Mid.Y + ")" + "\r\n";

            }
            else
            {
                if (OwnRobotsMean.Y > 0)
                {
                    if (Distance(packet.detection.robots_yellow[OppAttackSupportId].x, OwnRobotsMean.X, packet.detection.robots_yellow[OppAttackSupportId].y, OwnRobotsMean.Y) < Distance(packet.detection.robots_yellow[OppFreeRoleId].x, OwnRobotsMean.X, packet.detection.robots_yellow[OppFreeRoleId].y, OwnRobotsMean.Y))
                    {
                        Final_Point_Support.X = OwnRobotsMean.X;
                        Final_Point_Support.Y = OwnRobotsMean.Y;
                        Final_Point_Mid.X = OwnRobotsMean.X;
                        Final_Point_Mid.Y = -9 * OwnRobotsStandardDeviation.Y / 5;
                    }
                    else
                    {
                        Final_Point_Support.X = OwnRobotsMean.X;
                        Final_Point_Support.Y = -9 * OwnRobotsStandardDeviation.Y / 5;
                        Final_Point_Mid.X = OwnRobotsMean.X;
                        Final_Point_Mid.Y = OwnRobotsMean.Y;
                    }
                }
                else
                {
                    if (Distance(packet.detection.robots_yellow[OppAttackSupportId].x, OwnRobotsMean.X, packet.detection.robots_yellow[OppAttackSupportId].y, -OwnRobotsMean.Y) < Distance(packet.detection.robots_yellow[OppFreeRoleId].x, OwnRobotsMean.X, packet.detection.robots_yellow[OppFreeRoleId].y, -OwnRobotsMean.Y))
                    {
                        Final_Point_Support.X = OwnRobotsMean.X;
                        Final_Point_Support.Y = -OwnRobotsMean.Y;
                        Final_Point_Mid.X = OwnRobotsMean.X;
                        Final_Point_Mid.Y = 9 * OwnRobotsStandardDeviation.Y / 5;
                    }
                    else
                    {
                        Final_Point_Support.X = OwnRobotsMean.X;
                        Final_Point_Support.Y = 9 * OwnRobotsStandardDeviation.Y / 5;
                        Final_Point_Mid.X = OwnRobotsMean.X;
                        Final_Point_Mid.Y = -OwnRobotsMean.Y;
                    }
                }


                Final_Point_Mid.W = (float)Math.Atan2(packet.detection.balls[0].y - Final_Point_Mid.Y, packet.detection.balls[0].x - Final_Point_Mid.X);
                Final_Point_Support.W = (float)Math.Atan2(packet.detection.balls[0].y - Final_Point_Support.Y, packet.detection.balls[0].x - Final_Point_Support.X);

                repo.OutData[OppAttackSupportId].X = Final_Point_Support.X;
                repo.OutData[OppAttackSupportId].Y = Final_Point_Support.Y;
                repo.OutData[OppAttackSupportId].W = Final_Point_Support.W;
                //DataHandler.WriteReference(Final_Point_Support, Attack_Supporter_Yellow);
                repo.OutData[OppFreeRoleId].X = Final_Point_Mid.X;
                repo.OutData[OppFreeRoleId].Y = Final_Point_Mid.Y;
                repo.OutData[OppFreeRoleId].W = Final_Point_Mid.W;
                //DataHandler.WriteReference(Final_Point_Mid, Free_Role_Yellow);

                //MessageBox.Show(Final_Point_Support.x.ToString() + ", " + Final_Point_Support.y.ToString() + "    " + Final_Point_Mid.x.ToString() + ", " + Final_Point_Mid.y.ToString());
                //tb_private.Text += "Yellow team's Attack Support ID " + Attack_Supporter_Yellow + " and Midfielder ID " + Free_Role_Yellow + " move into the free space" + "\r\n";
            }
        }

        public void Mark_Opp_Attack_Support(SSL_WrapperPacket packet, bool IsBlue)
        {
            IRobotInfo Final_Point_Marker = new RobotParameters(0, 0, 0);
            float Final_Angle = 0;
            float tmp_slope = 0;
            if (IsBlue)
            {
                tmp_slope = (packet.detection.robots_yellow[OppAttackSupportId].y - packet.detection.robots_yellow[OppAttackerId].y) / (packet.detection.robots_yellow[OppAttackSupportId].x - packet.detection.robots_yellow[OppAttackerId].x);
                if (float.IsInfinity(tmp_slope))
                    tmp_slope = 300;
                float x = 0;
                float y = 0;
                double ratio = 0.1666666;
                while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 300)
                {
                    if (packet.detection.robots_yellow[OppAttackSupportId].x <= packet.detection.robots_yellow[OppAttackerId].x)
                    {
                        if (tmp_slope > 9)
                        {
                            x = (float)(x + ratio);
                            y = (float)(y + ratio * tmp_slope);
                        }
                        else
                        {
                            x = x + 1;
                            y = y + tmp_slope;
                        }
                    }
                    else
                    {
                        if (tmp_slope > 9)
                        {
                            x = (float)(x - ratio);
                            y = (float)(y - ratio * tmp_slope);
                        }
                        else
                        {
                            x = x - 1;
                            y = y - tmp_slope;
                        }
                    }
                }
                Final_Point_Marker.X = packet.detection.robots_yellow[OppAttackSupportId].x + x;
                Final_Point_Marker.Y = packet.detection.robots_yellow[OppAttackSupportId].y + y;
                Final_Angle = (float)Math.Atan2(packet.detection.robots_yellow[OppAttackSupportId].y - packet.detection.robots_yellow[OppAttackerId].y, packet.detection.robots_yellow[OppAttackSupportId].x - packet.detection.robots_yellow[OppAttackerId].x);
                Final_Angle = (float)(Final_Angle + Math.PI);
                Final_Point_Marker.W = Final_Angle;
                //MessageBox.Show(Final_Angle.ToString());

                repo.OutData[OwnAttackSupportId].X = Final_Point_Marker.X;
                repo.OutData[OwnAttackSupportId].Y = Final_Point_Marker.Y;
                repo.OutData[OwnAttackSupportId].W = Final_Point_Marker.W;
                //DataHandler.WriteReference(Final_Point_Marker, Attack_Supporter_Blue);


                //tb_private.Text += "Blue Marker ID " + Marker_Blue + " blocking goal " + "\r\n";
                //tb_private.Text += "Blue Midfielder ID " + Free_Role_Blue + " moves to (" + Final_Point_Marker.X + ", " + Final_Point_Marker.Y + ")" + "\r\n";
            }
            else
            {
                tmp_slope = (packet.detection.robots_blue[OwnAttackSupportId].y - packet.detection.robots_blue[OwnAttackerId].y) / (packet.detection.robots_blue[OwnAttackSupportId].x - packet.detection.robots_blue[OwnAttackerId].x);
                //.Show(tmp_slope.ToString());
                if (float.IsInfinity(tmp_slope))
                    tmp_slope = 300;
                //MessageBox.Show(tmp_slope.ToString());
                float x = 0;
                float y = 0;
                double ratio = 0.1666666666666;
                while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 300)
                {
                    if (packet.detection.robots_blue[OwnAttackSupportId].x <= packet.detection.robots_blue[OwnAttackerId].x)
                    {
                        if (tmp_slope > 9)
                        {
                            x = (float)(x + ratio);
                            y = (float)(y + ratio * tmp_slope);
                        }
                        else
                        {
                            x = x + 1;
                            y = y + tmp_slope;
                        }
                    }
                    else
                    {
                        if (tmp_slope > 9)
                        {
                            x = (float)(x - ratio);
                            y = (float)(y - ratio * tmp_slope);
                        }
                        else
                        {
                            x = x - 1;
                            y = y - tmp_slope;
                        }
                        //MessageBox.Show(x.ToString() + ", " + y.ToString());
                    }
                }
                Final_Point_Marker.X = packet.detection.robots_blue[OwnAttackSupportId].x + x;
                Final_Point_Marker.Y = packet.detection.robots_blue[OwnAttackSupportId].y + y;
                Final_Angle = (float)Math.Atan2(packet.detection.robots_blue[OwnAttackSupportId].y - packet.detection.robots_blue[OwnAttackerId].y, packet.detection.robots_blue[OwnAttackSupportId].x - packet.detection.robots_blue[OwnAttackerId].x);
                Final_Angle = (float)(Final_Angle + Math.PI);
                Final_Point_Marker.W = Final_Angle;
                //MessageBox.Show(Final_Angle.ToString());

                repo.OutData[OppAttackSupportId].X = Final_Point_Marker.X;
                repo.OutData[OppAttackSupportId].Y = Final_Point_Marker.Y;
                repo.OutData[OppAttackSupportId].W = Final_Point_Marker.W;
                //DataHandler.WriteReference(Final_Point_Marker, Attack_Supporter_Yellow);

                //tb_private.Text += "Yellow team's Midfielder ID " + Free_Role_Blue + " marking Blue's Attack Supporter ID " + Attack_Supporter_Blue + "\r\n";
            }
        }

        public IRobotInfo PassCalculation(IRobotInfo target, IRobotInfo source, double Confidence)
        {
            double disX = source.X - target.X;
            double disY = source.Y - target.Y;
            double ShootDistance = Math.Sqrt(Math.Pow(disX, 2) + Math.Pow(disY, 2));
            double ShootAngle = Math.Atan2(disY, disX);
            ShootDistance += Confidence;
            disX = ShootDistance * Math.Cos(ShootAngle) + target.X;
            disY = ShootDistance * Math.Sin(ShootAngle) + target.Y;
            return new RobotParameters((float)disX, (float)disY, (float)(Math.PI + Math.Atan2((disY - target.Y), (disX - target.X))));
            //DataHandler.WriteReference(new IRobotInfo((float)disX,(float) disY, (float)(Math.PI + Math.Atan2((disY - target.y), (disX - target.x)))),(int)source.robot_id);

        }

        public void GoalieCalculation(SSL_DetectionBall ball, int id)
        {
            IRobotInfo final = new RobotParameters(0, 0, 0);
            final.Y = 350 * (ball.y / 2025);
            final.X = -2975 + (500 * ((ball.x + 3025) / (6050)));
            final.W = (float)Math.Atan2((ball.y - final.Y), (ball.x - final.X));
            repo.OutData[id].X = final.X;
            repo.OutData[id].Y = final.Y;
            repo.OutData[id].W = final.W;
            //DataHandler.WriteReference(final, id);
        }

        public bool TriangleTest(IRobotInfo p1, IRobotInfo p2, IRobotInfo p3, IRobotInfo p)
        {
            float alpha = ((p2.Y - p3.Y) * (p.X - p3.X) + (p3.X - p2.X) * (p.Y - p3.Y)) /
        ((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
            float beta = ((p3.Y - p1.Y) * (p.X - p3.X) + (p1.X - p3.X) * (p.Y - p3.Y)) /
                   ((p2.Y - p3.Y) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Y - p3.Y));
            float gamma = 1.0f - alpha - beta;
            if (alpha > 0 && beta > 0 && gamma > 0)
                return true;
            else
                return false;
        }

        public IRobotInfo[] TrianglePoints(SSL_WrapperPacket packet, IRobotInfo source, int TargetID, bool IsBlue)
        {
            IRobotInfo[] t = new IRobotInfo[2];
            float angle = (float)Math.Atan2(source.Y - packet.detection.robots_blue[TargetID].y, source.X - packet.detection.robots_blue[TargetID].x);
            t[0] = new RobotParameters((float)(-100 * Math.Sin(angle)) + source.X, (float)(100 * Math.Cos(angle)) + source.Y, 0);
            t[1] = new RobotParameters((float)(100 * Math.Sin(angle)) + source.X, (float)(-100 * Math.Cos(angle)) + source.Y, 0);
            return t;
        }

        public void CheckandPass(SSL_WrapperPacket packet, bool IsBlue)
        {
            IRobotInfo roboinfo = RobotToIRobotInfo(repo.InData.Own(OwnAttackerId));
            IRobotInfo roboinfo2 = RobotToIRobotInfo(repo.InData.Own(OwnAttackSupportId));
            if (roboinfo.XVelocity <= 0.005 && roboinfo.YVelocity <= 0.005 && roboinfo.WVelocity <= 0.005 && roboinfo2.XVelocity <= 0.005 && roboinfo2.YVelocity <= 0.005 && roboinfo2.WVelocity < 0.005)
            {
                bool go_support = true;
                if (IsBlue)
                {
                    IRobotInfo[] pts = TrianglePoints(packet, PassCalculation(RobotToIRobotInfo(packet.detection.robots_blue[OwnAttackSupportId]), new RobotParameters(packet.detection.balls[0].x, packet.detection.balls[0].y, 0), 50), OwnAttackSupportId, repo.Configuration.IsBlueTeam);
                    foreach (SSL_DetectionRobot robot in packet.detection.robots_yellow)
                        if (TriangleTest(pts[0], pts[1], RobotToIRobotInfo(packet.detection.robots_blue[OwnAttackSupportId]), RobotToIRobotInfo(robot)))
                        {
                            go_support = false;
                            break;
                        }

                    if (go_support)
                    {
                        GoNearandRotate(packet, repo.Configuration.IsBlueTeam, 300, RobotToIRobotInfo(packet.detection.robots_blue[OwnAttackSupportId]));
                    }
                    else
                    {
                        go_support = true;
                        pts = TrianglePoints(packet, PassCalculation(RobotToIRobotInfo(packet.detection.robots_blue[OwnFreeRoleId]), new RobotParameters(packet.detection.balls[0].x, packet.detection.balls[0].y, 0), 50), OwnFreeRoleId, repo.Configuration.IsBlueTeam);
                        foreach (SSL_DetectionRobot robot in packet.detection.robots_yellow)
                        {
                            if (TriangleTest(pts[0], pts[1], RobotToIRobotInfo(packet.detection.robots_blue[OwnFreeRoleId]), RobotToIRobotInfo(robot)))
                            {
                                go_support = false;
                                break;
                            }
                        }
                        if (go_support)
                        {
                            GoNearandRotate(packet, repo.Configuration.IsBlueTeam, 300, RobotToIRobotInfo(packet.detection.robots_blue[OwnFreeRoleId]));
                        }
                    }
                }
            }
        }
        #endregion

        #region Basic_Functions
        /// <summary>
        /// Calculate Distance between two points.
        /// </summary>
        /// <param name="x1"> x value of point 1.</param>
        /// <param name="x2"> x value of point 2.</param>
        /// <param name="y1"> y value of point 1.</param>
        /// <param name="y2"> y value of point 2.</param>
        /// <returns> returns the distance between the points.</returns>
        private float Distance(float x1, float x2, float y1, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        /// <summary>
        /// Converts SSL detection Robot to Robot Info. Normally to use the location of robot in functions.
        /// </summary>
        /// <param name="robot"> SSL Detection robot to be converted.</param>
        /// <returns> returns the converted robot of type RobotInfo.</returns>
        public IRobotInfo RobotToIRobotInfo(SSL_DetectionRobot robot)
        {
            IRobotInfo newRobot = new RobotParameters();
            newRobot.X = robot.x;
            newRobot.Y = robot.y;
            newRobot.W = robot.orientation;
            return newRobot;
            //return new IRobotInfo(robot.x, robot.y, robot.orientation);
        }

        /// <summary>
        /// Converts SSL detection Ball to Robot Info.  Normally to use the location of ball in functions.
        /// </summary>
        /// <param name="ball"> SSL Detection ball to be converted.</param>
        /// <returns> returns the converted ball of type RobotInfo.</returns>
        public IRobotInfo BallToIRobotInfo(SSL_DetectionBall ball)
        {
            return new RobotParameters(ball.x, ball.y, 0);
        }

        /// <summary>
        /// Used to move a robot to a point with specific angle (w).
        /// </summary>
        /// <param name="ID"> Id of robot to move.</param>
        /// <param name="source"> The Position of Robot to move.</param>
        /// <param name="Destination"> The point (x,y,w) of the destination.</param>
        /// <param name="confidence"> The confidence value that is considered as close to destination.</param>
        /// <returns> returns false if the robot is not withing the confidence of the destination otherwise true.</returns>
        public bool GotoPoint(int ID, IRobotInfo source, IRobotInfo Destination, float confidence)
        {
            if (Distance(source.X, Destination.X, source.Y, Destination.Y) > confidence)
            {
                repo.OutData[ID].X = Destination.X;
                repo.OutData[ID].Y = Destination.Y;
                repo.OutData[ID].W = Destination.W;
                //DataHandler.WriteReference(Destination, ID);
                return false;
            }
            else
            { return true; }
        }

        /// <summary>
        /// Used to move a robot in a circle of fixed radius.
        /// </summary>
        /// <param name="ID"> Id of robot to be moved.</param>
        /// <param name="source"> Location of Robot to be moved.</param>
        /// <param name="center"> The center point of the circle.</param>
        /// <param name="target"> the point , when robot is facing will stop to move in circle.</param>
        /// <param name="radius"> The Radius of the circle to move in.</param>
        /// <returns>returns true when robot is facing the target otherwise returns false.</returns>
        public bool RotateArroundPoint(int ID, IRobotInfo source, IRobotInfo center, IRobotInfo target, float radius)
        {
            float finalangle = (float)Math.Atan2(center.Y - target.Y, center.X - target.X);
            IRobotInfo rb1 = new RobotParameters();
            float currentangle = (float)Math.Atan2(source.Y - center.Y, source.X - center.X);
            float Offset = 1.30f;       // 75degrees
            //if (Math.Abs(finalangle - currentangle) > Math.PI)
            //    Offset = -Offset;
            if (((finalangle) - (currentangle)) > (Offset))
            {
                currentangle += Offset;
                rb1.X = center.X + radius * (float)Math.Cos(currentangle);
                rb1.Y = center.Y + radius * (float)Math.Sin(currentangle);
                repo.OutData[ID].X = rb1.X;
                repo.OutData[ID].Y = rb1.Y;
                repo.OutData[ID].W = (float)Math.Atan2(target.Y - center.Y, target.X - center.X);
                //DataHandler.WriteReference(new IRobotInfo(rb1.X, rb1.Y, (float)Math.Atan2(target.y - center.y, target.x - center.x)), ID);
                return false;
            }
            else if (((finalangle) - (currentangle)) > (0.05f))
            {
                rb1.X = center.X + radius * (float)Math.Cos(finalangle);
                rb1.Y = center.Y + radius * (float)Math.Sin(finalangle);
                repo.OutData[ID].X = rb1.X;
                repo.OutData[ID].Y = rb1.Y;
                repo.OutData[ID].W = (float)Math.Atan2(target.Y - center.Y, target.X - center.X);
                //DataHandler.WriteReference(new IRobotInfo(rb1.X, rb1.Y, (float)Math.Atan2(target.y - center.y, target.x - center.x)), ID);
                return false;
            }
            else
            { return true; }
        }

        /// <summary>
        /// Used to go to ball turn on the dribbler to grab the ball.
        /// </summary>
        /// <param name="ID"> Id of robot to move.</param>
        /// <param name="source"> Location of Robot to be moved.</param>
        /// <param name="ball"> Location of ball to grab.</param>
        /// <returns>returns true when reached and grabbed ball else returns fale.</returns>
        public bool GrabBall(int ID, IRobotInfo source, SSL_DetectionBall ball)
        {
            if (Distance(source.X, ball.x, source.Y, ball.y) > 105)
            {
                GotoPoint(ID, source, new RobotParameters(ball.x, ball.y, (float)Math.Atan2((ball.y - source.Y), (ball.x - source.X))), 0);
                return false;
            }
            else
            {
                repo.OutData[ID].Grab = true;
                //DataHandler.Dribble[ID] = true;
                return true;
            }
        }

        /// <summary>
        /// Used to check if the ball is withing kicking radius and kicks the ball.
        /// </summary>
        /// <param name="ID"> Id of robot to kick the ball.</param>
        /// <param name="source"> Location of robot that will kick the ball.</param>
        /// <param name="ball"> Location of ball.</param>
        /// <param name="KickSpeed"> Speed by which ball will be kicked.</param>
        /// <returns> returns true of ball is in radius of kicking and is kicked otherwise false.</returns>
        public bool KickBall(int ID, IRobotInfo source, SSL_DetectionBall ball, float KickSpeed)
        {
            if (Distance(source.X, ball.x, source.Y, ball.y) < 105)
            {
                repo.OutData[ID].KickSpeed = KickSpeed;
                //DataHandler.WriteKickSpeed(KickSpeed, ID);
                return true;
            }
            else
            { return false; }
        }
        #endregion

        #region Derived_Functions
        // Apperently Not in use :/
        // used for marking a robot, with given ID and a confidence to maintain distance, to stand inbetween target and defence and facing a particular point
        public bool MarkOpponent(int ID, IRobotInfo source, IRobotInfo target, IRobotInfo defence, bool IsFacingTarget, float Confidence)
        {
            float r = Distance(defence.X, source.X, defence.Y, source.Y);
            float theeta = (float)Math.Atan2(target.Y - defence.Y, target.X - defence.X);
            float newx = defence.X + (r - Confidence) * (float)Math.Cos(theeta);
            float newy = defence.Y + (r - Confidence) * (float)Math.Sin(theeta);
            if (!IsFacingTarget)
            { theeta = -theeta; }
            if (Distance(source.X, newx, source.Y, newy) > 100)
            {
                repo.OutData[ID].X = newx;
                repo.OutData[ID].Y = newy;
                repo.OutData[ID].W = theeta;
                //DataHandler.WriteReference(new IRobotInfo(newx, newy, theeta), ID);
                return false;
            }
            else
            { return true; }
        }

        /// <summary>
        /// CMDragons Research Paper implementation for defence with 3 robots so that no one can shoot directly.
        /// </summary>
        /// <param name="IDG"> Own Goal Keeper ID.</param>
        /// <param name="IDD1"> Own Defender 1 ID.</param>
        /// <param name="IDD2"> Own Defender 2 ID.</param>
        /// <param name="Goalkeeper"> Location of Own Goal keeper.</param>
        /// <param name="Defender1"> Location of Own Defender 1.</param>
        /// <param name="Defender2"> Location of Own Defender 2.</param>
        /// <param name="ball"> Location of Ball.</param>
        /// <param name="OwnGoal"> Location of Own Goal.</param>
        public void BlockOppLineOfSight(int IDG, int IDD1, int IDD2, IRobotInfo Goalkeeper, IRobotInfo Defender1, IRobotInfo Defender2, SSL_DetectionBall ball, IRobotInfo OwnGoal)
        {
            List<float> slopes = new List<float>();
            IRobotInfo[] final_points = new IRobotInfo[3];
            List<IRobotInfo> points_goal = new List<IRobotInfo>();
            List<float> final_angles = new List<float>();
            int point_with_small_y = 0, point_with_big_y = 2;

            points_goal.Add(new RobotParameters(OwnGoal.X, OwnGoal.Y - 232, 0));
            points_goal.Add(OwnGoal);
            points_goal.Add(new RobotParameters(OwnGoal.X, OwnGoal.Y + 232, 0));

            IRobotInfo tmp_point = new RobotParameters(-3025, 0, 0);
            //tmp_point.Y = -232;
            //points_goal.Add(DataHandler.DeepClone<IRobotInfo>(tmp_point));
            //tmp_point.Y = 0;
            //points_goal.Add(DataHandler.DeepClone<IRobotInfo>(tmp_point));
            //tmp_point.Y = 232;
            //points_goal.Add(DataHandler.DeepClone<IRobotInfo>(tmp_point));

            slopes.Add((OwnGoal.Y - 232 - ball.y) / (OwnGoal.X - ball.x));
            slopes.Add((OwnGoal.Y + 0 - ball.y) / (OwnGoal.X - ball.x));
            slopes.Add((OwnGoal.Y + 232 - ball.y) / (OwnGoal.X - ball.x));
            float x = 0, y = 0;
            float ratio = 0.1666666666f;
            for (int i = 0; i < 3; i++)
            {
                if (float.IsInfinity(slopes[i]))
                    slopes[i] = 300;

                if (i == 1)
                {
                    while (Distance(x, 0, y, 0) < 375)
                    //while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 375)
                    {
                        if (slopes[i] > 9)
                        {
                            x = (float)(x + ratio);
                            y = (float)(y + ratio * slopes[i]);
                        }
                        else
                        {
                            x = x + 1;
                            y = y + slopes[i];
                        }
                    }
                }
                else
                {
                    while (Distance(x, 0, y, 0) < 800)
                    //while (Math.Sqrt(Math.Pow(x, 2) + Math.Pow(y, 2)) < 800)
                    {
                        if (slopes[i] > 9)
                        {
                            x = (float)(x + ratio);
                            y = (float)(y + ratio * slopes[i]);
                        }
                        else
                        {
                            x = x + 1;
                            y = y + slopes[i];
                        }
                    }
                }

                tmp_point.X = points_goal[i].X + x;
                tmp_point.Y = points_goal[i].Y + y;
                final_points[i] = Statics.DeepClone<IRobotInfo>(tmp_point);
                x = 0;
                y = 0;
            }

            final_angles.Add((float)Math.Atan2(ball.y - (OwnGoal.Y - 232), ball.x - OwnGoal.X));
            final_angles.Add((float)Math.Atan2(ball.y - (OwnGoal.Y - 0), ball.x - OwnGoal.X));
            final_angles.Add((float)Math.Atan2(ball.y - (OwnGoal.Y + 232), ball.x - OwnGoal.X));

            for (int i = 0; i < 3; i++)
            {
                final_points[i].W = final_angles[i];
            }

            if (final_points[2].Y < final_points[0].Y)
            {
                point_with_small_y = 2;
                point_with_big_y = 0;
            }

            if (Defender1.Y < Defender2.Y)
            {
                repo.OutData[IDD1].X = final_points[point_with_small_y].X;
                repo.OutData[IDD1].Y = final_points[point_with_small_y].Y;
                repo.OutData[IDD1].W = final_points[point_with_small_y].W;
                //DataHandler.WriteReference(final_points[point_with_small_y], IDD1);
                repo.OutData[IDG].X = final_points[1].X;        //Techincally this should be outside as it is same in if and else both 
                repo.OutData[IDG].Y = final_points[1].Y;
                repo.OutData[IDG].W = final_points[1].W;
                //DataHandler.WriteReference(final_points[1], IDG);
                repo.OutData[IDD2].X = final_points[point_with_big_y].X;
                repo.OutData[IDD2].Y = final_points[point_with_big_y].Y;
                repo.OutData[IDD2].W = final_points[point_with_big_y].W;
                //DataHandler.WriteReference(final_points[point_with_big_y], IDD2);
            }
            else
            {
                repo.OutData[IDD1].X = final_points[point_with_big_y].X;
                repo.OutData[IDD1].Y = final_points[point_with_big_y].Y;
                repo.OutData[IDD1].W = final_points[point_with_big_y].W;
                //DataHandler.WriteReference(final_points[point_with_big_y], IDD1);
                repo.OutData[IDG].X = final_points[1].X;
                repo.OutData[IDG].Y = final_points[1].Y;
                repo.OutData[IDG].W = final_points[1].W;
                //DataHandler.WriteReference(final_points[1], IDG);
                repo.OutData[IDD2].X = final_points[point_with_small_y].X;
                repo.OutData[IDD2].Y = final_points[point_with_small_y].Y;
                repo.OutData[IDD2].W = final_points[point_with_small_y].W;
                //DataHandler.WriteReference(final_points[point_with_small_y], IDD2);
            }
        }

        /// <summary>
        /// Used to send two robots i.e. AttackSupport and FreeRole to make space a head for pass/Attack.
        /// </summary>
        /// <param name="IDAS"> Attack Support Robot Id.</param>
        /// <param name="IDFR"> Free Role Robot Id.</param>
        /// <param name="AttackSupport"> Attack Support Robot Position.</param>
        /// <param name="FreeRole"> Free Role Robot Position.</param>
        /// <param name="ball"> Ball Postion.</param>
        /// <param name="OppMean"> Mean of Opponents All Robots.</param>
        /// <param name="OppSD"> Standard Deviation of Opponents All Robots.</param>
        /// <returns> Returns true if function Executes Successfully.</returns>
        public bool MakeFreeSpace(int IDAS, int IDFR, IRobotInfo AttackSupport, IRobotInfo FreeRole, SSL_DetectionBall ball, PointF OppMean, PointF OppSD)
        {
            IRobotInfo Final_Point_Support = new RobotParameters();
            IRobotInfo Final_Point_Mid = new RobotParameters();

            if (OppMean.Y > 0)
            {
                if (Distance(AttackSupport.X, OppMean.X, AttackSupport.Y, OppMean.Y) < Distance(FreeRole.X, OppMean.X, FreeRole.Y, OppMean.Y))
                {
                    Final_Point_Support.X = OppMean.X;
                    Final_Point_Support.Y = OppMean.Y;
                    Final_Point_Mid.X = OppMean.X;
                    Final_Point_Mid.Y = -9 * OppSD.Y / 5;
                }
                else
                {
                    Final_Point_Support.X = OppMean.X;
                    Final_Point_Support.Y = -9 * OppSD.Y / 5;
                    Final_Point_Mid.X = OppMean.X;
                    Final_Point_Mid.Y = OppMean.Y;
                }
            }
            else
            {
                if (Distance(AttackSupport.X, OppMean.X, AttackSupport.Y, -OppMean.Y) < Distance(FreeRole.X, OppMean.X, FreeRole.Y, -OppMean.Y))
                {
                    Final_Point_Support.X = OppMean.X;
                    Final_Point_Support.Y = -OppMean.Y;
                    Final_Point_Mid.X = OppMean.X;
                    Final_Point_Mid.Y = 9 * OppSD.Y / 5;
                }
                else
                {
                    Final_Point_Support.X = OppMean.X;
                    Final_Point_Support.Y = 9 * OppSD.Y / 5;
                    Final_Point_Mid.X = OppMean.X;
                    Final_Point_Mid.Y = -OppMean.Y;
                }
            }
            Final_Point_Support.W = (float)Math.Atan2(ball.y - Final_Point_Support.Y, ball.x - Final_Point_Support.X);
            Final_Point_Mid.W = (float)Math.Atan2(ball.y - Final_Point_Mid.Y, ball.x - Final_Point_Mid.X);

            if (Distance(AttackSupport.X, Final_Point_Support.X, AttackSupport.Y, Final_Point_Support.Y) > 100 || Distance(FreeRole.X, Final_Point_Mid.X, FreeRole.Y, Final_Point_Mid.Y) > 100)
            {
                repo.OutData[IDAS].X = Final_Point_Support.X;
                repo.OutData[IDAS].Y = Final_Point_Support.Y;
                repo.OutData[IDAS].W = Final_Point_Support.W;
                //DataHandler.WriteReference(Final_Point_Support, IDAS);
                repo.OutData[IDFR].X = Final_Point_Mid.X;
                repo.OutData[IDFR].Y = Final_Point_Mid.Y;
                repo.OutData[IDFR].W = Final_Point_Mid.W;
                //DataHandler.WriteReference(Final_Point_Mid, IDFR);
                return false;
            }
            else
                return true;
        }

        /// <summary>
        /// Used to check line of sight between source point and target point for the checkpoints.
        /// </summary>
        /// <param name="Source"> The Starting point. Normally Position of a robot to give pass.</param>
        /// <param name="Target"> The Target point. Normally position of a robot to receive pass.</param>
        /// <param name="checkpoints"> Points to be checked . Normally position of all other robots.</param>
        /// <returns> returns true if line of Sight is NOT clear and false if it is Clear.</returns>            //Should be opposite :/
        public bool CheckLineOfSight(IRobotInfo Source, IRobotInfo Target, IRobotInfo[] checkpoints)
        {
            bool IsBlocked = false;
            float radius = 200;
            PointF[] points = new PointF[4];
            float angle = (float)Math.Atan2(Target.Y - Source.Y, Target.X - Source.X);
            points[0] = new PointF((float)(-radius * Math.Sin(angle)) + Source.X, (float)(radius * Math.Cos(angle)) + Source.Y);
            points[1] = new PointF((float)(radius * Math.Sin(angle)) + Source.X, (float)(-radius * Math.Cos(angle)) + Source.Y);
            points[3] = new PointF((float)(-radius * Math.Sin(angle)) + Target.X, (float)(radius * Math.Cos(angle)) + Target.Y);
            points[2] = new PointF((float)(radius * Math.Sin(angle)) + Target.X, (float)(-radius * Math.Cos(angle)) + Target.Y);
            foreach (IRobotInfo checkpoint in checkpoints)
            {
                IsBlocked = IsPointInPolygon(points, checkpoint);
                if (IsBlocked)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Check_Functions
        /// <summary>
        /// Checks for a point weather it lies inside a polygon or not.
        /// Normally used to find out if line of sight is clear for a pass or a goal.
        /// </summary>
        /// <param name="polygon"> Array of points that make a polygon. Must be atleast 3 points.</param>
        /// <param name="point"> The point to check if it lies inside the polygon.</param>
        /// <returns>returns true if point lies inside the polygon and false if it lies outside.</returns>
        public bool IsPointInPolygon(PointF[] polygon, IRobotInfo point)
        {
            bool isInside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
                (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    isInside = !isInside;   //Not sure if this is correct shouldnt it be just true? or a break statment after it
                }
            }
            return isInside;
        }
        #endregion

        #region Calculation_Functions
        // Not being used right now :/ 
        public IRobotInfo CalculateNearPoint(IRobotInfo SourcePoint, IRobotInfo ReferencePoint, float conf)
        {
            double disX = ReferencePoint.X - SourcePoint.X;
            double disY = ReferencePoint.Y - SourcePoint.X;
            double Distance = Math.Sqrt(Math.Pow(disX, 2) + Math.Pow(disY, 2));
            double Angle = Math.Atan2(disY, disX);
            Distance += conf;
            return new RobotParameters((float)(Distance * Math.Cos(Angle) + SourcePoint.X), (float)(Distance * Math.Sin(Angle) + SourcePoint.Y), (float)Angle);
        }

        // Not being used right now :/
        /// <summary>
        /// Returns Array of robots except the id's specified 
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="ids"></param>
        /// <returns></returns>
        public IRobotInfo[] AllRobotsExcept(int[] ids)
        {
            List<IRobotInfo> final = new List<IRobotInfo>();
            //if (repo.Configuration.IsBlueTeam)
            //{
            for (int i = 0; i < repo.InData.Own().Length; i++)
            {
                for (int j = 0; j < ids.Length; j++)
                {
                    if (repo.InData.Own(i).robot_id == ids[j])
                        break;
                    else
                    {
                        if (j == ids.Length - 1)
                        {
                            final.Add(RobotToIRobotInfo(repo.InData.Own(i)));
                        }
                    }
                }
            }
            for (int i = 0; i < repo.InData.Opponent().Length; i++)
            {
                final.Add(RobotToIRobotInfo(repo.InData.Opponent(i)));
            }
            //}
            //else
            //{
            //    for (int i = 0; i < repo.InData.Opponent().Length; i++)
            //    {
            //        for (int j = 0; j < ids.Length; j++)
            //        {
            //            if (repo.InData.Opponent(i).robot_id == ids[j])
            //                break;
            //            else
            //            {
            //                if (j == ids.Length - 1)
            //                {
            //                    final.Add(RobotToIRobotInfo(repo.InData.Opponent(i)));
            //                }
            //            }
            //        }
            //    }
            //    for (int i = 0; i < repo.InData.Own().Length ; i++)
            //    {
            //        final.Add(RobotToIRobotInfo(repo.InData.Own(i)));
            //    }
            //}
            return final.ToArray<IRobotInfo>();

        }
        #endregion

        #region Interface_Functions

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
            RoleAssignments();
            ActionAssignments();
        }

        public void Release()
        {

        }

        public GetNextTasks GetNext
        {
            get { return nextTask; }
            set { nextTask = value; }
        }

        public void Execute()
        {
            try
            {
                Plan();
            }
            catch (Exception)
            {
                throw;
            }
        }
        #endregion


        public IRobotInfo[] PlanExclusive(SSL_WrapperPacket mainPacket)
        {
            Plan();
            return null;
        }


        public void OnRefereeCommandChanged(SSL_Referee command)
        {
            refereCommand = command;
        }
    }
}
