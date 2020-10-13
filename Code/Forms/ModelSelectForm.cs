﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.IO;
using Cupscale.Main;

namespace Cupscale.Forms
{
    public partial class ModelSelectForm : Form
    {
        public Button modelBtn;
        public int modelNo;

        public ModelSelectForm(Button modelButton, int modelNumber)
        {
            InitializeComponent();
            //Show(this);
            //TopMost = true;
            //CenterToScreen();
            modelBtn = modelButton;
            modelNo = modelNumber;
            SelectLastUsed();
        }

        public string selectedModel { get; set; }

        private void ModelSelectForm_Load(object sender, EventArgs e)
        {
            var modelDir = Config.Get("modelPath");
            if (!Directory.Exists(modelDir))
            {
                MessageBox.Show("The saved model directory does not exist - Make sure you've set a models folder!");
                return;
            }

            var modelsDir = new DirectoryInfo(modelDir);
            BuildTree(modelsDir, modelTree.Nodes);
            modelTree.ExpandAll();
        }

        private async void SelectLastUsed()
        {
            if (!Directory.Exists(Config.Get("modelPath")))
                return;

            while (modelTree.Nodes.Count < 1)
                await Task.Delay(1);

            if (string.IsNullOrWhiteSpace(Program.currentModel1))
                modelTree.SelectedNode = modelTree.Nodes[0];
            else
                CheckNodesRecursive(modelTree.Nodes[0]);
        }

        private void CheckNodesRecursive(TreeNode parentNode)
        {
            if (parentNode.Text.Trim() == modelBtn.Text.Trim())
                modelTree.SelectedNode = parentNode;

            foreach (TreeNode subNode in parentNode.Nodes) CheckNodesRecursive(subNode);
        }

        private void BuildTree(DirectoryInfo directoryInfo, TreeNodeCollection addInMe)
        {
            var curNode = addInMe.Add(directoryInfo.Name);

            foreach (var file in directoryInfo.GetFiles())
                if (file.Extension == ".pth") // Hide any other file extension
                    curNode.Nodes.Add(file.FullName, Path.ChangeExtension(file.Name, null));
            foreach (var subdir in directoryInfo.GetDirectories())
                if (subdir.GetFiles("*.pth", SearchOption.AllDirectories).Length > 0
                ) // Don't list folders that have no PTH files
                    BuildTree(subdir, curNode.Nodes);
        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            selectedModel = modelTree.SelectedNode.Name;
            modelBtn.Text = Path.GetFileNameWithoutExtension(selectedModel);
            Logger.Log("Selected model " + modelBtn.Text + " - Full path: " + selectedModel);
            if (modelNo == 1) // idk if this could be less hardcoded?
                Program.currentModel1 = selectedModel;
            if (modelNo == 2)
                Program.currentModel2 = selectedModel;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void modelTree_AfterSelect(object sender, TreeViewEventArgs e)
        {
            confirmBtn.Enabled = Path.GetExtension(modelTree.SelectedNode.Name) == ".pth";
        }

        private void ModelSelectForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char) Keys.Enter)
            {
                e.Handled = true;
                if (confirmBtn.Enabled)
                    confirmBtn_Click(null, null);
            }

            if (e.KeyChar == (char) Keys.Escape)
            {
                e.Handled = true;
                Close();
            }
        }

        private void clearBtn_Click(object sender, EventArgs e)
        {
            selectedModel = "";
            modelBtn.Text = "None";
            if (modelNo == 1) // idk if this could be less hardcoded?
                Program.currentModel1 = null;
            if (modelNo == 2)
                Program.currentModel2 = null;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}