using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.Main;

namespace Cupscale.Forms
{
    public partial class DialogForm : Form
    {
        public DialogForm(string message, int selfDestructTime = 60)
        {
            InitializeComponent();
            Program.currentTemporaryForms.Add(this);
            mainLabel.Text = message;
            Show();
            //TopMost = true;
            SelfDestruct(selfDestructTime);
        }

        public void ChangeText(string s)
        {
            mainLabel.Text = s;
        }

        private async Task SelfDestruct(int time)
        {
            await Task.Delay(time * 1000);
            Close();
        }

        private void DialogForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Program.currentTemporaryForms.Remove(this);
        }
    }
}