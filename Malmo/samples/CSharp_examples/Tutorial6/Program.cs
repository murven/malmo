namespace Tutorial6
{
    using Microsoft.Research.Malmo;
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
        static Random random = new Random();
        static void Main(string[] args)
        {
            Console.Clear();
            var agent = new TabQAgent();
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



            //# -- set up the mission -- #
            var currentPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var missionFilename = Path.Combine(currentPath, "mission.xml");
            var missionString = System.IO.File.ReadAllText(missionFilename);
            MissionSpec mission = new MissionSpec(missionString, validate: true);
            for (int x = 1; x <= 4; x++)
            {
                for (int z = 1; z <= 13; z++)
                {
                    if (random.NextDouble() <= 0.1)
                    {
                        mission.drawBlock(x, 45, z, "lava");
                    }
                }
            }
            var maxRetries = 3;
            var numRepeats = 150;
            if (agentHost.receivedArgument("test"))
            {
                numRepeats = 1;
            }

            var cumulativeRewards = new List<double>();
            for (int i = 0; i < numRepeats; i++)
            {
                Console.WriteLine($"Repeat {i} of {numRepeats}");
                MissionRecordSpec missionRecord = new MissionRecordSpec();
                for (int retryIndex = 0; retryIndex < maxRetries; retryIndex++)
                {
                    try
                    {
                        agentHost.startMission(mission, missionRecord);
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
                        if (worldState.is_mission_running)
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retryIndex == maxRetries - 1)
                        {
                            Console.Error.WriteLine("Error starting mission: {0}", ex.Message);
                            Environment.Exit(1);
                        }
                        else
                        {
                            Thread.Sleep(2500);
                        }
                    }
                }
                var cumulativeReward = agent.Run(agentHost);
                Console.WriteLine($"Cumulative reward: {cumulativeReward}");
                cumulativeRewards.Add(cumulativeReward);
                Thread.Sleep(500); // (let the Mod reset)
            }
            Console.WriteLine("Done.");
            Console.WriteLine();
            Console.WriteLine($"Cumulative rewards for all {numRepeats} runs: {string.Join(",", cumulativeRewards)}");
        }
    }
}
