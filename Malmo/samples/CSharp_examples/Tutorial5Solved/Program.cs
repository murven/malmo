﻿namespace Tutorial5
{
    using Microsoft.Research.Malmo;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    class Program
    {
        static string GetCuboidWithVariant(int x1, int y1, int z1, int x2, int y2, int z2, string blockType, string variant)
        {
            return $"<DrawCuboid x1=\"{x1}\" y1=\"{y1}\" z1=\"{z1}\" x2=\"{x2}\" y2=\"{y2}\" z2=\"{z2}\" type=\"{blockType}\" variant=\"{variant}\"/>";
        }
        static string GetCuboid(int x1, int y1, int z1, int x2, int y2, int z2, string blockType)
        {
            return $"<DrawCuboid x1=\"{x1}\" y1=\"{y1}\" z1=\"{z1}\" x2=\"{x2}\" y2=\"{y2}\" z2=\"{z2}\" type=\"{blockType}\"/>";
        }
        static string Menger(int xorg, int yorg, int zorg, int size, string blockType, string variant, string holeType)
        {
            // Draw Solid Chunk
            var genString = GetCuboidWithVariant(xorg, yorg, zorg, xorg + size - 1, yorg + size - 1, zorg + size - 1, blockType, variant) + Environment.NewLine;
            // Now Remove holes
            var unit = size;
            int w, x, y, z;
            while (unit >= 3)
            {
                w = unit / 3;
                for (int i = 0; i < size; i += unit)
                {
                    for (int j = 0; j < size; j += unit)
                    {
                        x = xorg + i;
                        y = yorg + j;
                        genString += GetCuboid(x + w, y + w, zorg, (x + 2 * w) - 1, (y + 2 * w) - 1, zorg + size - 1, holeType) + Environment.NewLine;
                        y = yorg + i;
                        z = zorg + j;
                        genString += GetCuboid(xorg, y + w, z + w, xorg + size - 1, (y + 2 * w) - 1, (z + 2 * w) - 1, holeType) + Environment.NewLine;
                        genString += GetCuboid(x + w, yorg, z + w, (x + 2 * w) - 1, yorg + size - 1, (z + 2 * w) - 1, holeType) + Environment.NewLine;
                    }
                }
                unit /= 3;
            }
            return genString;
        }
        public static void Main()
        {
            AgentHost agentHost = new AgentHost();
            try
            {
                agentHost.parse(new StringVector(Environment.GetCommandLineArgs()));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: {0}", ex.Message);
                Console.Error.WriteLine(agentHost.getUsage());
                Environment.Exit(1);
            }
            if (agentHost.receivedArgument("help"))
            {
                Console.Error.WriteLine(agentHost.getUsage());
                Environment.Exit(0);
            }


            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var missionFilename = Path.Combine(currentPath, "mission.xml");
            var missionString = System.IO.File.ReadAllText(missionFilename);
            missionString = missionString.Replace("<!--MENGER RESULT-->", Menger(-40, 40, -13, 27, "stone", "smooth_granite", "air"));

            MissionSpec mission = new MissionSpec(missionString, validate: true);

            MissionRecordSpec missionRecord = new MissionRecordSpec("./saved_data.tgz");
            missionRecord.recordCommands();
            missionRecord.recordMP4(20, 400000);
            missionRecord.recordRewards();
            missionRecord.recordObservations();

            try
            {
                agentHost.startMission(mission, missionRecord);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error starting mission: {0}", ex.Message);
                Environment.Exit(1);
            }

            WorldState worldState;

            Console.WriteLine("Waiting for the mission to start");
            do
            {
                Console.Write(".");
                Thread.Sleep(100);
                worldState = agentHost.getWorldState();

                foreach (TimestampedString error in worldState.errors) Console.Error.WriteLine("Error: {0}", error.text);
            }
            while (!worldState.is_mission_running);

            Console.WriteLine("Mission running...");

            //Possible solution for challenge set in tutorial 4
            agentHost.sendCommand("hotbar.9 1");
            agentHost.sendCommand("hotbar.9 0");

            agentHost.sendCommand("pitch 0.2");
            Thread.Sleep(1000);
            agentHost.sendCommand("pitch 0");
            agentHost.sendCommand("move 1");
            agentHost.sendCommand("attack 1");


            Random rand = new Random();
            var isJumping = false;
            // main loop:
            do
            {
                Thread.Sleep(500);
                worldState = agentHost.getWorldState();
                Console.WriteLine(
                    "video,observations,rewards received: {0}, {1}, {2}",
                    worldState.number_of_video_frames_since_last_state,
                    worldState.number_of_observations_since_last_state,
                    worldState.number_of_rewards_since_last_state);
                foreach (TimestampedReward reward in worldState.rewards) Console.Error.WriteLine("Summed reward: {0}", reward.getValue());
                foreach (TimestampedString error in worldState.errors) Console.Error.WriteLine("Error: {0}", error.text);
                if (worldState.number_of_observations_since_last_state > 0)
                {
                    var msg = worldState.observations[0].text;
                    var observations = JObject.Parse(msg);
                    var grid = observations["floor3x3"][0];
                    if (isJumping && grid[4].Value<string>() != "lava")
                    {
                        agentHost.sendCommand("jump 0");
                        isJumping = false;
                    }
                    if(grid[3].Value<string>()=="lava")
                    {
                        agentHost.sendCommand("jump 1");
                        isJumping = true;
                    }
                }
            }
            while (worldState.is_mission_running);

            Console.WriteLine("Mission has stopped.");
        }
    }
}
