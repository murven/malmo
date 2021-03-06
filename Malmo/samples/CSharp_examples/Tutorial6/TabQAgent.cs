﻿namespace Tutorial6
{
    using Microsoft.Research.Malmo;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    class TabQAgent
    {
        public double Epsilon { get; set; } = 0.1;
        public string[] Actions { get; set; } = new string[] { "movenorth 1", "movesouth 1", "movewest 1", "moveeast 1" };
        public Dictionary<string, Dictionary<string, double>> QTable { get; set; } = new Dictionary<string, Dictionary<string, double>>();
        public string PreviousState { get; set; }
        public string PreviousAction { get; set; }
        Random random = new Random();
        public void UpdateQTable(double reward)
        {
            //Change q_table to reflect what we have learnt.
            //retrieve the old action value from the Q-table (indexed by the previous state and the previous action)
            var oldQ = QTable[PreviousState][PreviousAction];
            //# TODO: what should the new action value be?
            var newQ = oldQ;
            //# assign the new action value to the Q-table
            QTable[PreviousState][PreviousAction] = newQ;
        }
        public void UpdateQTableFromTerminatingState(double reward)
        {
            //Change q_table to reflect what we have learnt.
            //retrieve the old action value from the Q-table (indexed by the previous state and the previous action)
            var oldQ = QTable[PreviousState][PreviousAction];
            //# TODO: what should the new action value be?
            var newQ = oldQ;
            //# assign the new action value to the Q-table
            QTable[PreviousState][PreviousAction] = newQ;
        }
        public void Act(WorldState worldState, AgentHost agentHost)
        {
            //"""take 1 action in response to the current world state"""
            var observationText = worldState.observations[0].text;
            dynamic observations = JObject.Parse(observationText);
            JToken XPos, ZPos;
            observations.TryGetValue("XPos", out XPos);
            observations.TryGetValue("ZPos", out ZPos);
            if (XPos == null || ZPos == null)
            {
                Console.WriteLine($"Incomplete observation received: {observationText}");
                return;
            }
            var currentState = $"{observations.XPos}:{observations.ZPos}";
            Console.WriteLine($"State: {currentState} (x = {XPos}, z = {ZPos})");
            if (!QTable.ContainsKey(currentState))
            {
                QTable[currentState] = new Dictionary<string, double>();
                foreach (var action in Actions)
                {
                    QTable[currentState][action] = 0.0;
                }
            }
            //# select the next action
            var actionIndex = 0;
            if (random.NextDouble() < Epsilon || QTable[currentState].Values.Count == 0)
            {
                actionIndex = random.Next(0, Actions.Length);
                Console.WriteLine($"Random action: {Actions[actionIndex]}");
            }
            else
            {
                var maxReward = QTable[currentState].Values.Count == 0 ? double.MinValue : QTable[currentState].Values.Max();
                Console.WriteLine($"Current values: {string.Join(",", QTable[currentState].Values)}");
                var l = new List<int>();
                for (int i = 0; i < Actions.Length; i++)
                {
                    if (QTable[currentState].ContainsKey(Actions[i]) && QTable[currentState][Actions[i]] == maxReward)
                    {
                        l.Add(i);
                    }
                }
                var y = random.Next(0, l.Count);
                actionIndex = l[y];
                Console.WriteLine($"Taking Q Action: {Actions[actionIndex]}");
            }

            try
            {
                //# try to send the selected action, only update prev_s if this succeeds
                agentHost.sendCommand(Actions[actionIndex]);
                Thread.Sleep(100);
                PreviousState = currentState;
                PreviousAction = Actions[actionIndex];
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed ot send command: {e}");
            }
        }
        public double Run(AgentHost agentHost)
        {
            var totalReward = 0.0;
            PreviousState = null;
            PreviousAction = null;
            var isFirstAction = true;

            var worldState = agentHost.getWorldState();
            do
            {
                var currentReward = 0.0;
                if (isFirstAction)
                {
                    while (true)
                    {
                        Thread.Sleep(100);
                        worldState = agentHost.getWorldState();
                        foreach (var error in worldState.errors)
                        {
                            Console.WriteLine($"Error: {error}");
                        }
                        foreach (var reward in worldState.rewards)
                        {
                            currentReward += reward.getValue();
                        }
                        if (worldState.is_mission_running && worldState.observations.Count > 0 && !(worldState.observations[0].text == "{}"))
                        {
                            Act(worldState, agentHost);
                            totalReward += currentReward;
                            break;
                        }
                        if (!worldState.is_mission_running) break;
                    }
                    isFirstAction = false;
                }
                else
                {
                    //        # wait for non-zero reward
                    while (worldState.is_mission_running && currentReward == 0.0)
                    {
                        Thread.Sleep(200);
                        worldState = agentHost.getWorldState();
                        foreach (var error in worldState.errors)
                        {
                            Console.WriteLine($"Error: {error}");
                        }
                        foreach (var reward in worldState.rewards)
                        {
                            currentReward += reward.getValue();
                        }
                    }
                    while (true)
                    {
                        Thread.Sleep(200);
                        worldState = agentHost.getWorldState();
                        foreach (var error in worldState.errors)
                        {
                            Console.WriteLine($"Error: {error}");
                        }
                        foreach (var reward in worldState.rewards)
                        {
                            currentReward += reward.getValue();
                        }
                        if (worldState.is_mission_running && worldState.observations.Count > 0 && !(worldState.observations[0].text == "{}"))
                        {
                            Act(worldState, agentHost);
                            totalReward += currentReward;
                            break;
                        }
                        if (!worldState.is_mission_running) break;
                    }
                }
                //# process final reward
                totalReward += currentReward;
                if (PreviousState != null && PreviousAction != null)
                {
                    UpdateQTable(totalReward);
                }
            } while (worldState.is_mission_running);
            UpdateQTableFromTerminatingState(totalReward);
            return totalReward;
        }
    }
}
