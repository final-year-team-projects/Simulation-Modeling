using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MultiQueueModels;
using MultiQueueTesting;

namespace MultiQueueSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SimulationSystem simulationSystem = new SimulationSystem();
        int simulationFinishTime = 0, currentQueueLength = 0;
        int[] queueFreq = new int[100000];
        List<int> freqServers = new List<int>();
        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 100000; i++)
                queueFreq[i] = 0;
        }

        int x = 0;
        int binarySearch(int rnd, List<TimeDistribution> lst)
        {
            int l = 0, r = lst.Count - 1, mid;
            x++;
            while(l <= r)
            {
                mid = (l + r) / 2;
                if (lst[mid].MinRange <= rnd && lst[mid].MaxRange >= rnd)
                    return lst[mid].Time;
                else if (lst[mid].MinRange < rnd && lst[mid].MaxRange < rnd)
                    l = mid + 1;
                else
                    r = mid - 1;
            }
            return 0;
        }

        Tuple<int, int> selectServer(int arrivalTime)
        {
            if((int)simulationSystem.SelectionMethod == 1)
            {
                int mn = 1000000000, ret = 0;
                for(int i = 0; i < simulationSystem.NumberOfServers; i++)
                {
                    if(simulationSystem.Servers[i].FinishTime < mn)
                    {
                        mn = simulationSystem.Servers[i].FinishTime;
                        ret = i;
                    }

                    if(simulationSystem.Servers[i].FinishTime <= arrivalTime)
                    {
                        mn = simulationSystem.Servers[i].FinishTime;
                        ret = i;
                        break;
                    }
                }
                return new Tuple<int, int>(Math.Max(0, mn), ret);
            }
            else if((int)simulationSystem.SelectionMethod == 2)
            {
                List<int> serversIDX = new List<int>();
                for (int i = 0; i < simulationSystem.NumberOfServers; i++)
                {
                    if (simulationSystem.Servers[i].FinishTime <= arrivalTime)
                        serversIDX.Add(i);
                }
                Random rnd = new Random();
                int ret = rnd.Next(0, serversIDX.Count - 1);
                return new Tuple<int, int>(Math.Max(0, simulationSystem.Servers[ret].FinishTime), ret);
            }
            else
            {

                int mn = simulationSystem.Servers[0].FinishTime, ret = 0;
                for (int i = 1; i < simulationSystem.NumberOfServers; i++)
                {
                    if (simulationSystem.Servers[i].FinishTime < mn)
                    {
                        mn = simulationSystem.Servers[i].FinishTime;
                        ret = i;
                    }
                }
                decimal mnU = 0;
                bool ut = false;
                for (int i = 0; i < simulationSystem.NumberOfServers; i++)
                {
                    if(arrivalTime >= simulationSystem.Servers[i].FinishTime)
                    {
                        if(!ut)
                        {
                            ut = true;
                            mnU = simulationSystem.Servers[i].Utilization;
                            ret = i;
                        }
                        else if(simulationSystem.Servers[i].Utilization < mnU)
                        {
                            mnU = simulationSystem.Servers[i].Utilization;
                            ret = i;
                        }
                    }
                }
                return new Tuple<int, int>(Math.Max(0, simulationSystem.Servers[ret].FinishTime), ret);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(openFileDialog1.FileName);
                    simulationSystem.NumberOfServers = Convert.ToInt32(lines[1]);
                    for (int i = 0; i < simulationSystem.NumberOfServers ; i++)
                    {
                        simulationSystem.Servers.Add(new Server());
                        simulationSystem.Servers[i].ID = i + 1;
                        simulationSystem.Servers[i].FinishTime = -1;
                        simulationSystem.Servers[i].TotalWorkingTime = 0;
                        freqServers.Add(0);
                    }
                    simulationSystem.StoppingNumber = Convert.ToInt32(lines[4]);
                    simulationSystem.StoppingCriteria = (Enums.StoppingCriteria)Convert.ToInt32(lines[7]);
                    simulationSystem.SelectionMethod = (Enums.SelectionMethod)Convert.ToInt32(lines[10]);
                    int currentLine = 13;
                    decimal cumulative = 0;
                    while (lines[currentLine] != "")
                    {
                        string[] separator = {", "};
                        string[] values = lines[currentLine].Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);
                        TimeDistribution td = new TimeDistribution();
                        td.Time = Convert.ToInt32(values[0]);
                        td.Probability = Convert.ToDecimal(values[1]);
                        td.MinRange = (int)(cumulative * 100 + 1);
                        cumulative += td.Probability;
                        td.MaxRange = (int)(cumulative * 100);
                        td.CummProbability = cumulative;
                        simulationSystem.InterarrivalDistribution.Add(td);
                        currentLine++;
                    }

                    for(int i = 0 ; i < simulationSystem.NumberOfServers ; i++)
                    {
                        currentLine += 2;
                        cumulative = 0;
                        while(currentLine < lines.Length && lines[currentLine] != "")
                        {
                            string[] separator = { ", " };
                            string[] values = lines[currentLine].Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);
                            TimeDistribution td = new TimeDistribution();
                            td.Time = Convert.ToInt32(values[0]);
                            td.Probability = Convert.ToDecimal(values[1]);
                            td.MinRange = (int)(cumulative * 100 + 1);
                            cumulative += td.Probability;
                            td.MaxRange = (int)(cumulative * 100);
                            td.CummProbability = cumulative;
                            simulationSystem.Servers[i].TimeDistribution.Add(td);
                            currentLine++;
                        }
                    }
                    Random rnd = new Random();
                    int tc = 0;
                    while((tc < simulationSystem.StoppingNumber && (int)simulationSystem.StoppingCriteria == 1) || (simulationFinishTime <= simulationSystem.StoppingNumber && (int)simulationSystem.StoppingCriteria == 2))
                    {
                        tc++;
                        SimulationCase simulationCase = new SimulationCase();
                        simulationCase.CustomerNumber = tc;
                        if(tc == 1)
                        {
                            simulationCase.ArrivalTime = 0;
                            simulationCase.RandomService = rnd.Next(1, 100);
                            simulationCase.StartTime = 0;
                            simulationCase.ServiceTime = binarySearch(simulationCase.RandomService, simulationSystem.Servers[0].TimeDistribution);
                            simulationCase.EndTime = simulationCase.StartTime + simulationCase.ServiceTime;
                            simulationFinishTime = Math.Max(simulationFinishTime, simulationCase.EndTime);
                            simulationSystem.Servers[0].FinishTime = simulationCase.EndTime;
                            simulationSystem.Servers[0].TotalWorkingTime += simulationCase.ServiceTime;
                            simulationCase.AssignedServer = simulationSystem.Servers[0];
                            freqServers[0]++;
                            simulationSystem.SimulationTable.Add(simulationCase);
                            continue;
                        }
                        simulationCase.RandomInterArrival = rnd.Next(1, 100);
                        simulationCase.InterArrival = binarySearch(simulationCase.RandomInterArrival, simulationSystem.InterarrivalDistribution);
                        simulationCase.ArrivalTime = simulationCase.InterArrival + simulationSystem.SimulationTable[tc - 2].ArrivalTime;
                        Tuple<int, int> bestServer = selectServer(simulationCase.ArrivalTime);
                        freqServers[bestServer.Item2]++;
                        simulationCase.AssignedServer = simulationSystem.Servers[bestServer.Item2];
                        simulationCase.RandomService = rnd.Next(1, 100);
                        simulationCase.ServiceTime = binarySearch(simulationCase.RandomService, simulationSystem.Servers[bestServer.Item2].TimeDistribution);
                        simulationCase.StartTime = Math.Max(simulationCase.ArrivalTime, bestServer.Item1);
                        simulationCase.EndTime = simulationCase.StartTime + simulationCase.ServiceTime;
                        simulationFinishTime = Math.Max(simulationFinishTime, simulationCase.EndTime);
                        simulationCase.TimeInQueue = simulationCase.StartTime - simulationCase.ArrivalTime;
                        if (simulationCase.TimeInQueue > 0)
                            simulationSystem.PerformanceMeasures.WaitingProbability++;
                        for(int i = simulationCase.ArrivalTime; i < simulationCase.StartTime; i++)
                        {
                            queueFreq[i]++;
                            simulationSystem.PerformanceMeasures.MaxQueueLength = Math.Max(simulationSystem.PerformanceMeasures.MaxQueueLength, queueFreq[i]);
                        }
                        simulationSystem.PerformanceMeasures.MaxQueueLength = Math.Max(simulationSystem.PerformanceMeasures.MaxQueueLength, currentQueueLength);
                        simulationSystem.PerformanceMeasures.AverageWaitingTime += simulationCase.TimeInQueue;
                        simulationSystem.Servers[bestServer.Item2].FinishTime = simulationCase.EndTime;
                        simulationSystem.Servers[bestServer.Item2].TotalWorkingTime += simulationCase.ServiceTime;
                        simulationSystem.SimulationTable.Add(simulationCase);
                    }
                    for(int i = 0; i < simulationSystem.NumberOfServers; i++)
                    {
                        simulationSystem.Servers[i].IdleProbability = (simulationFinishTime - simulationSystem.Servers[i].TotalWorkingTime) / (decimal)simulationFinishTime;
                        simulationSystem.Servers[i].AverageServiceTime = simulationSystem.Servers[i].TotalWorkingTime / (decimal)freqServers[i];
                        simulationSystem.Servers[i].Utilization = simulationSystem.Servers[i].TotalWorkingTime / (decimal)simulationFinishTime;
                    }
                    simulationSystem.PerformanceMeasures.AverageWaitingTime /= simulationSystem.StoppingNumber;
                    simulationSystem.PerformanceMeasures.WaitingProbability /= simulationSystem.StoppingNumber;
                    waitingLabel.Text = simulationSystem.PerformanceMeasures.AverageWaitingTime.ToString();
                    probabilityLabel.Text = simulationSystem.PerformanceMeasures.WaitingProbability.ToString();
                    queueLabel.Text = simulationSystem.PerformanceMeasures.MaxQueueLength.ToString();
                    serversOutput.DataSource = simulationSystem.Servers;
                    outputGrid.DataSource = simulationSystem.SimulationTable;
                    outputGrid.Columns.Remove("AssignedServer");
                    outputGrid.Columns.Add("ServerID", "ServerID");
                    for (int i = 0; i < simulationSystem.StoppingNumber; i++)
                        outputGrid[outputGrid.Columns.Count - 1, i].Value = simulationSystem.SimulationTable[i].AssignedServer.ID;
                }
                catch
                {
                    MessageBox.Show("You have selected a worng file!");
                }
                simulationSystem.SimulationTable[0].RandomInterArrival = 1;
                string testingResult = TestingManager.Test(simulationSystem, Constants.FileNames.TestCase1);
                MessageBox.Show(testingResult);
            }
        }
    }
}
