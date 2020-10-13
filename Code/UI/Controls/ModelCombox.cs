using Cupscale;

namespace System.Windows.Forms
{
    internal class ModelCombox : ComboBox
    {
        //bool initialized = false;

        public ModelCombox() // Constructor
        {
            base.Text = "Open the dropdown to select a model.";
        }

        /*
        protected override void OnVisibleChanged(EventArgs e)
        {
			
            if (!IsRunning())
                return;
            base.Text = "running";

            base.OnVisibleChanged(e);

            if (!initialized)
            {
            UIHelpers.FillModelComboBox(this, false);
            initialized = true;
            }
			
        }
        */

        protected override void OnDropDown(EventArgs e)
        {
            // if (!IsRunning())
            //    return;
            base.OnDropDown(e);
            UIHelpers.FillModelComboBox(this);
        }

        /*
        bool IsRunning()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Runtime;
        }
		*/
    }
}