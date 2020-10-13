using System;
using System.Windows.Forms;
using Cupscale.UI;

namespace Cupscale.Forms
{
    public partial class AdvancedModelsForm : Form
    {
        private readonly string leftModelName;
        private readonly string rightModelName;

        public AdvancedModelsForm(string leftModel, string rightModel)
        {
            leftModelName = leftModel;
            rightModelName = rightModel;
            InitializeComponent();
            //Show();
            CenterToParent();
        }

        private void InterpForm_Load(object sender, EventArgs e)
        {
            interpSlider.Value = MainUIHelper.interpValue / 5;
            UpdateLabels();
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            MainUIHelper.interpValue = interpSlider.Value * 5;
            Close();
        }

        private void interpSlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateLabels();
        }

        private void UpdateLabels()
        {
            leftModelLabel.Text = leftModelName + ": " + (100 - interpSlider.Value * 5) + "%";
            rightModelLabel.Text = rightModelName + ": " + interpSlider.Value * 5 + "%";
        }
    }
}