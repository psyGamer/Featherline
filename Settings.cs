﻿using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Windows.Forms;
using System;
using System.Text.RegularExpressions;
using System.Linq;

namespace Featherline
{
    partial class Form1
    {
        public static FileStream configFile = new FileStream("settings.xml", FileMode.OpenOrCreate);

        public void LoadSettings(ref Settings s)
        {
            try {
                s = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(configFile);
            }
            catch {//SaveSettings();
                s = new Settings();
            }

            num_framecount.Value = settings.Framecount;
            num_population.Value = settings.Population;
            num_generations.Value = settings.Generations;
            num_survivorCount.Value = settings.SurvivorCount;
            num_population_ValueChanged(null, null);

            num_mutationProbability.Value = (decimal)settings.MutationProbability;
            num_crossoverProbability.Value = (decimal)settings.CrossoverProbability;
            num_simplificationProbability.Value = (decimal)settings.SimplificationProbability;

            num_mutationMagnitude.Value = (decimal)settings.MutationMagnitude;
            num_mutChangeCount.Value = settings.MaxMutChangeCount;

            txt_infoFile.Text = settings.InfoFile;
            txt_initSolution.Text = settings.Favorite;

            LoadCheckpoints();
            LoadManualHitboxes();

            cbx_inputLinesMode.Checked = settings.LimitInputLinesMode;
            num_inputLineCount.Value = settings.InputLineCount;
            num_inputLineCount.Maximum = num_framecount.Value;
            cbx_inputLinesMode_CheckedChanged(null, null);

            txt_customSpinners.Text = settings.customSpinnerNames;

            cbx_avoidWalls.Checked = settings.AvoidWalls;
            cbx_enableSteepTurns.Checked = settings.EnableSteepTurns;
        }

        public void SaveSettings()
        {
            num_inputLineCount.Maximum = num_framecount.Value;

            // put information into the settings object
            settings.InfoFile = txt_infoFile.Text;
            settings.Favorite = txt_initSolution?.Text ?? null;

            settings.Generations = (int)num_generations.Value;
            settings.Population = (int)num_population.Value;
            settings.SurvivorCount = (int)num_survivorCount.Value;
            settings.Framecount = (int)num_framecount.Value;

            settings.MutationProbability = (float)num_mutationProbability.Value;
            settings.CrossoverProbability = (float)num_crossoverProbability.Value;
            settings.SimplificationProbability = (float)num_simplificationProbability.Value;

            settings.MutationMagnitude = (float)num_mutationMagnitude.Value;
            settings.MaxMutChangeCount = (int)num_mutChangeCount.Value;

            SaveCheckpoints();
            SaveManualHitboxes();

            settings.LimitInputLinesMode = cbx_inputLinesMode.Checked;
            settings.InputLineCount = (int)num_inputLineCount.Value;

            settings.customSpinnerNames = txt_customSpinners.Text;

            settings.AvoidWalls = cbx_avoidWalls.Checked;
            settings.EnableSteepTurns = cbx_enableSteepTurns.Checked;

            // reset the config file and serialize
            configFile.SetLength(0);
            new XmlSerializer(typeof(Settings)).Serialize(configFile, settings);
        }

        public void LoadCheckpoints()
        {
            if (!(settings.Checkpoints is null))
                foreach (var cp in settings.Checkpoints)
                    grd_checkpoints.Rows.Add(cp.L + 2, cp.U - 4, cp.R - 3, cp.D - 9);
        }

        public void SaveCheckpoints()
        {
            var cps = new List<Checkpoint>();
            foreach (DataGridViewRow cp in grd_checkpoints.Rows) {
                if (cp.Cells.Count == 4 && cp.Cells[0].Value != null && cp.Cells[1].Value != null) {
                    try {
                        if (cp.Cells[2].Value != null && cp.Cells[3].Value != null) {
                            cps.Add(new Checkpoint(
                                Level.ProcessInput(cp.Cells[0].Value.ToString()),
                                Level.ProcessInput(cp.Cells[1].Value.ToString()),
                                Level.ProcessInput(cp.Cells[2].Value.ToString()),
                                Level.ProcessInput(cp.Cells[3].Value.ToString())
                            ));
                        }
                        else {
                            var point = new IntVec2(
                                -8 + Level.ProcessInput(cp.Cells[0].Value.ToString()),
                                -8 + Level.ProcessInput(cp.Cells[1].Value.ToString()));

                            float closestDist = 9999;
                            IntVec2 closest = null;
                            bool closestIsSwitch = false;
                            
                            foreach (var v2 in Level.Feathers) {
                                float dist = v2.Dist(point);
                                if (dist < closestDist && dist < 24)
                                    (closestDist, closest) = (dist, v2);
                            }

                            foreach (var v2 in Level.Switches) {
                                float dist = v2.Dist(point);
                                if (dist < closestDist && dist < 24)
                                    (closestDist, closest, closestIsSwitch) = (dist, v2, true);
                            }

                            if (closest is null)
                                throw new ArgumentException("Error: Could not find nearby feather or touch switch for checkpoint coordinates.");

                            cps.Add(closestIsSwitch
                                ? new Checkpoint(closest.X - 7, closest.Y - 7, closest.X + 15 + 7, closest.Y + 15 + 7)
                                : new Checkpoint(closest.X, closest.Y, closest.X + 19, closest.Y + 19)
                            );
                        }
                    } finally { }
                }
            }
            settings.Checkpoints = cps.ToArray();
        }

        private string GetCustomSpinnerNames() => Regex.Matches(txt_customSpinners.Text, @"\S+")
            .Aggregate("", (res, m) => res + $"{{{m.Value}.Position}}");

        private void LoadManualHitboxes()
        {
            if (!(settings.manualHitboxes is null))
                foreach (var hb in settings.manualHitboxes)
                    grd_manualHitboxes.Rows.Add(hb);
        }

        private void SaveManualHitboxes()
        {
            var res = new List<string[]>();
            foreach (DataGridViewRow hb in grd_manualHitboxes.Rows) {
                if (hb.Cells[0].Value != null && hb.Cells[1].Value != null && hb.Cells[2].Value != null && hb.Cells[3].Value != null) {
                    try {
                        res.Add(new string[] {
                            (string)hb.Cells[0].Value,
                            (string)hb.Cells[1].Value,
                            (string)hb.Cells[2].Value,
                            (string)hb.Cells[3].Value,
                            (string)hb.Cells[4].Value
                        });
                    } finally { }
                }
            }
            settings.manualHitboxes = res.ToArray();
        }
    }

    public class Settings
    {
        public static DataGridView manualHBSrc;

        public string InfoFile;

        public string Favorite;
        public int Framecount = 120;

        public int Population = 50;
        public int Generations = 2000;
        public int SurvivorCount = 20;

        public float StartX;
        public float StartY;

        public bool DefineStartBoost;
        public float BoostX;
        public float BoostY;

        public float CrossoverProbability = 1;
        public float MutationProbability = 2;
        public float SimplificationProbability = 1;

        public float MutationMagnitude = 5;
        public int MaxMutChangeCount = 5;

        public Checkpoint[] Checkpoints;

        public bool LimitInputLinesMode = false;
        public int InputLineCount = 12;

        public string customSpinnerNames;

        public bool AvoidWalls = true;
        public bool EnableSteepTurns = false;

        public string[][] manualHitboxes;


        public Settings Copy() => (Settings)MemberwiseClone();
    }
}