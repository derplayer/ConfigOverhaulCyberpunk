using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Runtime.InteropServices;

using IniParser;
using IniParser.Model;

namespace ConfigOverhaulCyberpunk
{
    public partial class MainForm : Form
    {
        BindingList<REDEngineFeature> featureList;
        string path = @".\engine\config\platform\pc\user.ini";
        FileIniDataParser parser = new FileIniDataParser();
        IniData redConfig;

        public MainForm()
        {
            InitializeComponent();
            bool pathOk = VerifyPath();

            if(pathOk == false)
            {
                OpenFileDialog choofdlog = new OpenFileDialog();
                choofdlog.Filter = "REDprelauncher (REDprelauncher.exe)|REDprelauncher.exe";
                choofdlog.FilterIndex = 1;
                choofdlog.Multiselect = false;

                if (choofdlog.ShowDialog() == DialogResult.OK)
                {
                    string sFileName = Path.GetDirectoryName(choofdlog.FileName);
                    path = sFileName + @"\engine\config\platform\pc\user.ini";
                }
            }

            this.Load += new EventHandler(Form1_Load);
        }

        public bool VerifyPath()
        {
            bool checkEngineDir = Directory.Exists(@".\engine");
            if (checkEngineDir) return true;

            bool checkDefaultSteamDir = Directory.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077\");
            if (checkDefaultSteamDir)
            {
                path = @"C:\Program Files (x86)\Steam\steamapps\common\Cyberpunk 2077\engine\config\platform\pc\user.ini";
                return true;
            }

            return false;
        }

        void Form1_Load(object sender, EventArgs e)
        {
            //Verify path integrity
            this.Load += new EventHandler(Form1_Load);

            InitializeFeatureList();
            dataGridViewMain.DataSource = featureList;
            dataGridViewMain.Columns["Enabled"].Width = 50;
            dataGridViewMain.Columns["GroupName"].ReadOnly = true;
            dataGridViewMain.Columns["GroupName"].Width = 200;
            dataGridViewMain.Columns["FunctionName"].ReadOnly = true;
            dataGridViewMain.Columns["FunctionName"].Width = 200;
            dataGridViewMain.Columns["Description"].ReadOnly = false;

            dataGridViewMain.ClearSelection();
        }

        private void dataGridView_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            //foreach (DataGridViewRow row in dataGridViewMain.SelectedRows)
            //{
            //    REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[row.Index];

            //    if (current.Enabled == true)
            //        current.Enabled = false;
            //    else
            //        current.Enabled = true;
            //}

            dataGridViewMain.Refresh();
            dataGridViewMain.ClearSelection();
        }

        private void InitializeFeatureList()
        {
            if (File.Exists(path))
            {
                //Load already generated INI
                redConfig = parser.ReadFile(path);
            }
            else
            {
                //Create base file with all features enabled
                redConfig = new IniData();
                var featureDefinitions = LoadREDFeatures();

                foreach (var featureDef in featureDefinitions)
                {
                    var kd = new KeyData(featureDef.FunctionName);
                    kd.Value = "true";
                    if (featureDef.Description != "") kd.Comments.Add(featureDef.Description);

                    if (featureDef.GroupName.Contains("Developer"))
                    {
                        redConfig.Sections.AddSection(featureDef.GroupName);
                        redConfig[featureDef.GroupName].AddKey(kd);
                    }

                    if (featureDef.GroupName.Contains("Rendering"))
                    {
                        redConfig.Sections.AddSection(featureDef.GroupName);
                        redConfig[featureDef.GroupName].AddKey(kd);
                    }

                    if (featureDef.GroupName.Contains("RayTracing"))
                    {
                        redConfig.Sections.AddSection(featureDef.GroupName);
                        redConfig[featureDef.GroupName].AddKey(kd);
                    }
                }

                File.WriteAllText(path, redConfig.ToString());
            }

            featureList = new BindingList<REDEngineFeature>();

            //Initialize DataGrid
            foreach (var section in redConfig.Sections)
            {
                foreach (var featureSet in section.Keys)
                {
                    var featureTmp = new REDEngineFeature();
                    featureTmp.Enabled = Convert.ToBoolean(featureSet.Value);
                    featureTmp.GroupName = section.SectionName;
                    featureTmp.FunctionName = featureSet.KeyName;
                    if (featureSet.Comments.Count >= 1)
                        featureTmp.Description = featureSet.Comments[0];
                    featureList.Add(featureTmp);
                }
            }
        }

        public List<REDEngineFeature> LoadREDFeatures()
        {
            List<REDEngineFeature> list = new List<REDEngineFeature>();
            List<string> lines = Properties.Resources.FeatureList.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            ).ToList();

            foreach (var line in lines)
            {
                try
                {
                    string[] fields = line.Split(';');
                    if (fields.Count() >= 3)
                    {
                        var feature = new REDEngineFeature
                        {
                            Enabled = true,
                            GroupName = fields[0],
                            FunctionName = fields[1],
                            Description = fields[2]
                        };

                        list.Add(feature);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Something is wrong with a feature definition: " + e.ToString());
                }
            }

            return list;
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            IniData redConfigNew = new IniData();
            int disableCount = 0;
            //datagrid to ini
            foreach (DataGridViewRow row in dataGridViewMain.Rows)
            {
                REDEngineFeature featureDef = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[row.Index];

                var kd = new KeyData(featureDef.FunctionName);
                kd.Value = featureDef.Enabled.ToString();

                if (featureDef.Enabled == false) disableCount++;

                if (featureDef.Description != "") kd.Comments.Add(featureDef.Description);

                if (featureDef.GroupName.Contains("Developer"))
                {
                    redConfigNew.Sections.AddSection(featureDef.GroupName);
                    redConfigNew[featureDef.GroupName].AddKey(kd);
                }

                if (featureDef.GroupName.Contains("Rendering"))
                {
                    redConfigNew.Sections.AddSection(featureDef.GroupName);
                    redConfigNew[featureDef.GroupName].AddKey(kd);
                }

                if (featureDef.GroupName.Contains("RayTracing"))
                {
                    redConfig.Sections.AddSection(featureDef.GroupName);
                    redConfig[featureDef.GroupName].AddKey(kd);
                }
            }

            //when everything is enabled, just clear the user.ini
            if(disableCount >= 1)
                File.WriteAllText(path, redConfigNew.ToString());
            else
                File.Delete(path);
        }

        public class REDEngineFeature
        {
            public bool Enabled { get; set; }
            public string GroupName { get; set; }
            public string FunctionName { get; set; }
            public string Description { get; set; }
        }

        private void VeryLowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (var row in dataGridViewMain.Rows)
            {
                REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[i];

                current.Enabled = false;
                if (current.FunctionName.Contains("DistantShadows")) current.Enabled = true;
                if (current.FunctionName.Contains("GlobalIllumination")) current.Enabled = true;
                i++;
            }
            dataGridViewMain.Refresh();
        }

        private void lowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (var row in dataGridViewMain.Rows)
            {
                REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[i];
                current.Enabled = false;
                if (current.FunctionName.Contains("DistantShadows")) current.Enabled = true;
                if (current.FunctionName.Contains("GlobalIllumination")) current.Enabled = true;
                if (current.FunctionName.Contains("Hair")) current.Enabled = true;
                if (current.FunctionName.Contains("RainMap")) current.Enabled = true;
                if (current.FunctionName.Contains("Weather")) current.Enabled = true;
                if (current.FunctionName.Contains("DynamicDecals")) current.Enabled = true;
                if (current.FunctionName.Contains("UseSkinningLOD")) current.Enabled = true;

                //more demanding options
                if (current.FunctionName.Contains("SSAO") && current.GroupName.Contains("Feature")) current.Enabled = true;
                if (current.FunctionName.Contains("Antialiasing")) current.Enabled = true;

                i++;
            }
            dataGridViewMain.Refresh();
        }

        private void optimizedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (var row in dataGridViewMain.Rows)
            {
                REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[i];
                current.Enabled = true;

                if (current.FunctionName.Contains("Character")) current.Enabled = false;
                if (current.FunctionName.Contains("DistantFog")) current.Enabled = false;
                if (current.FunctionName.Contains("DistantGI")) current.Enabled = false;
                if (current.FunctionName.Contains("DistantVolFog")) current.Enabled = false;
                if (current.FunctionName.Contains("MotionBlur")) current.Enabled = false;
                if (current.FunctionName.Contains("Runtime")) current.Enabled = false;
                if (current.FunctionName.Contains("ScreenSpace")) current.Enabled = false;
                if (current.FunctionName.Contains("VolumetricClouds")) current.Enabled = false;
                if (current.FunctionName.Contains("VolumetricFog")) current.Enabled = false;
                if (current.FunctionName.Contains("UseExperimentalVolFog")) current.Enabled = false;
                i++;
            }
            disableAsyncComputeNVIDIAToolStripMenuItem_Click(null, null);
            dataGridViewMain.Refresh();
        }

        private void disableAsyncComputeNVIDIAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (var row in dataGridViewMain.Rows)
            {
                REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[i];
                if(current.GroupName.Contains("Async")) current.Enabled = false;
                i++;
            }
            dataGridViewMain.Refresh();
        }

        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int i = 0;
            foreach (var row in dataGridViewMain.Rows)
            {
                REDEngineFeature current = ((BindingList<REDEngineFeature>)dataGridViewMain.DataSource)[i];
                current.Enabled = true;
                i++;
            }
            dataGridViewMain.Refresh();
        }
    }
}
