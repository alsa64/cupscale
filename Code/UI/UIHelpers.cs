using System;
using System.Drawing;
using System.Windows.Forms;
using Cupscale.UI;
using Cyotek.Windows.Forms;

namespace Cupscale
{
    internal class UIHelpers
    {
        public static void InitCombox(ComboBox box, int index)
        {
            if (box.Items.Count >= 1)
            {
                box.SelectedIndex = index;
                box.Text = box.Items[index].ToString();
            }
        }

        public static void FillModelComboBox(ComboBox box, bool resetIndex = false)
        {
            EsrganData.ReloadModelList();
            box.Items.Clear();
            foreach (var model in EsrganData.models) box.Items.Add(model);
            if (resetIndex || string.IsNullOrEmpty(box.Text)) InitCombox(box, 0);
        }

        public static void FillEnumComboBox(ComboBox box, Type type, int defaultIndex = 0)
        {
            box.Items.Clear();
            foreach (var format in Enum.GetNames(type)) box.Items.Add(format.TitleCase());
            InitCombox(box, defaultIndex);
        }

        public static void ReplaceImageAtSameScale(ImageBox imgBox, Image newImg)
        {
            //Logger.Log("Replacing image on " + imgBox.Name + " with new image (" + newImg.Width + "x" + newImg.Height + ")");
            var num = imgBox.Image.Width / (float) newImg.Width;
            var num2 = imgBox.AutoScrollPosition.X / num;
            var num3 = imgBox.AutoScrollPosition.Y / num;
            if (num2 < 0f) num2 *= -1f;
            if (num3 < 0f) num3 *= -1f;
            num2 *= num;
            num3 *= num;
            imgBox.Image = newImg;
            imgBox.Zoom = (int) Math.Round(imgBox.Zoom * num);
            var autoScrollPosition = imgBox.AutoScrollPosition;
            autoScrollPosition.X = (int) num2;
            autoScrollPosition.Y = (int) num3;
            imgBox.AutoScrollPosition = autoScrollPosition;
        }
    }
}