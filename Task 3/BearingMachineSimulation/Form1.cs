using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using BearingMachineTesting;
using BearingMachineModels;
using System.IO;

namespace BearingMachineSimulation
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        SimulationSystem simulationSystem = new SimulationSystem();
        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string testingResult = TestingManager.Test(simulationSystem, Constants.FileNames.TestCase1);
            MessageBox.Show(testingResult);
        }
        
        int binarySearch(int rnd, List<TimeDistribution> lst)
        {
            int l = 0, r = lst.Count - 1, mid;
            while (l <= r)
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
        
        void TakeInput()
        {
            string[] lines = File.ReadAllLines(openFileDialog1.FileName);
            simulationSystem.DowntimeCost = Convert.ToInt32(lines[1]);
            simulationSystem.RepairPersonCost = Convert.ToInt32(lines[4]);
            simulationSystem.BearingCost = Convert.ToInt32(lines[7]);
            simulationSystem.NumberOfHours = Convert.ToInt32(lines[10]);
            simulationSystem.NumberOfBearings = Convert.ToInt32(lines[13]);
            simulationSystem.RepairTimeForOneBearing = Convert.ToInt32(lines[16]);
            simulationSystem.RepairTimeForAllBearings = Convert.ToInt32(lines[19]);
            int i = 22;
            decimal lst = 0;
            for (; lines[i] != ""; i++)
            {
                string[] separator = { ", " };
                string[] values = lines[i].Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);
                TimeDistribution td = new TimeDistribution();
                td.Time = Convert.ToInt32(values[0]);
                td.Probability = Convert.ToDecimal(values[1]);
                td.CummProbability = lst + td.Probability;
                td.MinRange = Convert.ToInt32(lst * 100) + 1;
                lst = td.CummProbability;
                td.MaxRange = Convert.ToInt32(lst * 100);
                simulationSystem.DelayTimeDistribution.Add(td);
            }
            i += 2;
            lst = 0;
            for (; i < lines.Length; i++)
            {
                string[] separator = { ", " };
                string[] values = lines[i].Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);
                TimeDistribution td = new TimeDistribution();
                td.Time = Convert.ToInt32(values[0]);
                td.Probability = Convert.ToDecimal(values[1]);
                td.CummProbability = lst + td.Probability;
                td.MinRange = Convert.ToInt32(lst * 100) + 1;
                lst = td.CummProbability;
                td.MaxRange = Convert.ToInt32(lst * 100);
                simulationSystem.BearingLifeDistribution.Add(td);
            }
        }
        void BuildTable1()
        {
            int delayt = 0;

            DataTable dc = new DataTable(), dc2 = new DataTable(), dc3 = new DataTable();
            dc.Columns.Add("Bearing Index");
            dc2.Columns.Add("Bearing rnd");
            dc3.Columns.Add("bearing life");
            Random rnd = new Random();
            for (int bearingIDX = 1; bearingIDX <= simulationSystem.NumberOfBearings; bearingIDX++)
            {
                for (int life = 0; life < simulationSystem.NumberOfHours;)
                {
                    CurrentSimulationCase csc = new CurrentSimulationCase();
                    csc.Bearing.Index = bearingIDX;
                    dc.Rows.Add(bearingIDX);
                    csc.Bearing.RandomHours= rnd.Next(1, 100);
                    dc2.Rows.Add(csc.Bearing.RandomHours);
                    csc.Bearing.Hours = binarySearch(csc.Bearing.RandomHours, simulationSystem.BearingLifeDistribution);
                    dc3.Rows.Add(csc.Bearing.Hours);
                    int lstIdx = simulationSystem.CurrentSimulationTable.Count;
                    csc.RandomDelay = rnd.Next(1, 100);
                    csc.AccumulatedHours = csc.Bearing.Hours + life;
                    csc.Delay = binarySearch(csc.RandomDelay, simulationSystem.DelayTimeDistribution);
                    simulationSystem.CurrentSimulationTable.Add(csc);
                    delayt += csc.Delay;
                    life = csc.AccumulatedHours;
                }
            }
            simulationSystem.CurrentPerformanceMeasures.BearingCost = simulationSystem.CurrentSimulationTable.Count*simulationSystem.BearingCost;
            simulationSystem.CurrentPerformanceMeasures.DelayCost = delayt * simulationSystem.DowntimeCost;
            simulationSystem.CurrentPerformanceMeasures.DowntimeCost = simulationSystem.CurrentSimulationTable.Count * simulationSystem.RepairTimeForOneBearing* simulationSystem.DowntimeCost;
            simulationSystem.CurrentPerformanceMeasures.RepairPersonCost = (decimal)simulationSystem.CurrentSimulationTable.Count * simulationSystem.RepairTimeForOneBearing * simulationSystem.RepairPersonCost / 60;

            simulationSystem.CurrentPerformanceMeasures.TotalCost = simulationSystem.CurrentPerformanceMeasures.BearingCost + simulationSystem.CurrentPerformanceMeasures.DelayCost + simulationSystem.CurrentPerformanceMeasures.DowntimeCost + simulationSystem.CurrentPerformanceMeasures.RepairPersonCost;
            dataGridView1.DataSource = simulationSystem.CurrentSimulationTable;
            dataGridView1.Columns[0].Visible = false;
            dataGridView1.Columns.Add("bearingIDX", "bearingIDX");
            dataGridView1.Columns.Add("bearingRND", "bearingRND");
            dataGridView1.Columns.Add("bearingLife", "bearingLife");
            dataGridView1.Columns["bearingIDX"].DisplayIndex = 0;
            dataGridView1.Columns["bearingRND"].DisplayIndex = 1;
            dataGridView1.Columns["bearingLife"].DisplayIndex = 2;
            for (int i = 0; i < simulationSystem.CurrentSimulationTable.Count; i++)
            {
                dataGridView1[dataGridView1.Columns.Count - 3, i].Value = Convert.ToInt32(dc.Rows[i][0]);
                dataGridView1[dataGridView1.Columns.Count - 2, i].Value = Convert.ToInt32(dc2.Rows[i][0]);
                dataGridView1[dataGridView1.Columns.Count - 1, i].Value = Convert.ToInt32(dc3.Rows[i][0]);
            }
        }
        void BuildTable2()
        {
            List<List<Bearing>> bearingsCategorized = new List<List<Bearing>>();
            int bearingIDX = 0;
            foreach(CurrentSimulationCase csc in simulationSystem.CurrentSimulationTable)
            {
                Bearing b = csc.Bearing;
                if (b.Index != bearingIDX)
                {
                    bearingsCategorized.Add(new List<Bearing>());
                    bearingIDX++;
                }
                bearingsCategorized[bearingsCategorized.Count - 1].Add(b);
            }

            int delayt = 0;
            Random rnd = new Random();
            for (int life = 0, j = 0; life < simulationSystem.NumberOfHours; j++)
            {
                ProposedSimulationCase psc = new ProposedSimulationCase();
                psc.RandomDelay = rnd.Next(1, 100);
                psc.Delay = binarySearch(psc.RandomDelay, simulationSystem.DelayTimeDistribution);
                delayt += psc.Delay;
                psc.AccumulatedHours = life;
                psc.FirstFailure = int.MaxValue;
                for (bearingIDX = 0; bearingIDX < simulationSystem.NumberOfBearings; bearingIDX++)
                {
                    if(bearingsCategorized[bearingIDX].Count <= j)
                    {
                        Bearing b = new Bearing();
                        b.Index = bearingIDX + 1;
                        b.RandomHours = rnd.Next(1, 100);
                        b.Hours = binarySearch(b.RandomHours, simulationSystem.BearingLifeDistribution);
                        psc.Bearings.Add(b);
                        psc.FirstFailure = Math.Min(b.Hours, psc.FirstFailure);
                    }
                    else
                    {
                        psc.Bearings.Add(bearingsCategorized[bearingIDX][j]);
                        psc.FirstFailure = Math.Min(bearingsCategorized[bearingIDX][j].Hours, psc.FirstFailure);
                    }
                }
                psc.AccumulatedHours += psc.FirstFailure;
                life = psc.AccumulatedHours;
                simulationSystem.ProposedSimulationTable.Add(psc);
            }
            simulationSystem.ProposedPerformanceMeasures.BearingCost = simulationSystem.ProposedSimulationTable.Count * simulationSystem.BearingCost * simulationSystem.NumberOfBearings;
            simulationSystem.ProposedPerformanceMeasures.DelayCost = delayt * simulationSystem.DowntimeCost;
            simulationSystem.ProposedPerformanceMeasures.DowntimeCost = simulationSystem.ProposedSimulationTable.Count * simulationSystem.RepairTimeForAllBearings * simulationSystem.DowntimeCost;
            simulationSystem.ProposedPerformanceMeasures.RepairPersonCost = (decimal)simulationSystem.ProposedSimulationTable.Count * simulationSystem.RepairTimeForAllBearings * simulationSystem.RepairPersonCost / 60;

            simulationSystem.ProposedPerformanceMeasures.TotalCost = simulationSystem.ProposedPerformanceMeasures.BearingCost 
                + simulationSystem.ProposedPerformanceMeasures.DelayCost + simulationSystem.ProposedPerformanceMeasures.DowntimeCost 
                + simulationSystem.ProposedPerformanceMeasures.RepairPersonCost;

            dataGridView2.DataSource = simulationSystem.ProposedSimulationTable;
            for (int i = 1; i <= simulationSystem.NumberOfBearings; i++)
                dataGridView3.Columns.Add("Bearing" + i.ToString() + "Life", "Bearing " + i.ToString() + " Life");
            dataGridView3.RowCount = simulationSystem.ProposedSimulationTable.Count;
            for (int i = 0; i < simulationSystem.ProposedSimulationTable.Count; i++)
            {
                for (int j = 0; j < simulationSystem.NumberOfBearings; j++)
                    dataGridView3[j, i].Value = simulationSystem.ProposedSimulationTable[i].Bearings[j].Hours;
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    TakeInput();
                    BuildTable1();
                    BuildTable2();
                }
                catch
                {
                    MessageBox.Show("You have selected a worng file!");
                }
            }
        }
    }
}
