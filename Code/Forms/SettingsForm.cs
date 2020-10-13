﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Cupscale.IO;
using Cupscale.Main;

namespace Cupscale.Forms
{
    public partial class SettingsForm : Form
    {
        private bool initialized;

        public SettingsForm()
        {
            InitializeComponent();
            Show();
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            Program.mainForm.Enabled = false;
            Logger.textbox = logTbox;
            LoadSettings();
            initialized = true;
        }

        private void LoadSettings()
        {
            // ESRGAN/Cupscale
            Config.LoadComboxIndex(esrganVersion);
            Config.LoadGuiElement(tilesize);
            Config.LoadGuiElement(alpha);
            Config.LoadComboxIndex(seamlessMode);
            Config.LoadGuiElement(modelPath);
            Config.LoadGuiElement(alphaBgColor);
            Config.LoadGuiElement(jpegExtension);
            Config.LoadComboxIndex(cudaFallback);
            Config.LoadComboxIndex(previewFormat);
            Config.LoadGuiElement(reloadImageBeforeUpscale);
            // Formats
            Config.LoadGuiElement(jpegQ);
            Config.LoadGuiElement(webpQ);
            Config.LoadGuiElement(dxtMode);
            Config.LoadGuiElement(ddsEnableMips);
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveSettings();
            Program.mainForm.Enabled = true;
        }

        private void SaveSettings()
        {
            // ESRGAN/Cupscale
            Config.SaveComboxIndex(esrganVersion);
            Config.SaveGuiElement(tilesize, true);
            Config.SaveGuiElement(alpha);
            Config.SaveComboxIndex(seamlessMode);
            Config.SaveGuiElement(modelPath);
            Config.SaveGuiElement(alphaBgColor);
            Config.SaveGuiElement(jpegExtension);
            Config.SaveComboxIndex(cudaFallback);
            Config.SaveComboxIndex(previewFormat);
            Config.SaveGuiElement(reloadImageBeforeUpscale);

            // Formats
            Config.SaveGuiElement(jpegQ, true);
            Config.SaveGuiElement(webpQ, true);
            Config.SaveGuiElement(dxtMode);
            Config.SaveGuiElement(ddsEnableMips);
        }

        private void confAlphaBgColorBtn_Click(object sender, EventArgs e)
        {
            alphaBgColorDialog.ShowDialog();
            var colorStr = ColorTranslator.ToHtml(Color.FromArgb(alphaBgColorDialog.Color.ToArgb())).Replace("#", "") +
                           "FF";
            alphaBgColor.Text = colorStr;
            Config.Set("alphaBgColor", colorStr);
        }

        private void logTbox_VisibleChanged(object sender, EventArgs e)
        {
            if (logTbox.Visible)
                logTbox.Text = Logger.GetSessionLog();
        }

        private void selectModelsPathBtn_Click(object sender, EventArgs e)
        {
            modelsPathDialog.ShowDialog();
            modelPath.Text = modelsPathDialog.SelectedPath;
        }

        private async void reinstallOverwriteBtn_Click(object sender, EventArgs e)
        {
            await ShippedEsrgan.Install();
            BringToFront();
        }

        private async void reinstallCleanBtn_Click(object sender, EventArgs e)
        {
            ShippedEsrgan.Uninstall(false);
            await ShippedEsrgan.Install();
            BringToFront();
        }

        private void uninstallResBtn_Click(object sender, EventArgs e)
        {
            ShippedEsrgan.Uninstall(false);
            MessageBox.Show(
                "Uninstalled resources.\nYou can now delete Cupscale.exe if you want to completely remove it from your PC.\n" +
                "However, your settings file was not deleted.", "Message");
            Program.Quit();
        }

        private void uninstallFullBtn_Click(object sender, EventArgs e)
        {
            Close();
            ShippedEsrgan.Uninstall(true);
            MessageBox.Show(
                "Uninstalled all files.\nYou can now delete Cupscale.exe if you want to completely remove it from your PC.",
                "Message");
            Program.Quit();
        }

        private void esrganVersion_SelectedIndexChanged(object sender, EventArgs e)
        {
            seamlessMode.Enabled = esrganVersion.SelectedIndex == 1;
        }

        private void cudaFallback_SelectedIndexChanged(object sender, EventArgs e)
        {
            //MessageBox.Show("This only serves as a fallback mode.\nDon't use this if you have an Nvidia GPU.\n\n" +
            //"The following features do not work with Vulkan/NCNN:\n- Model Interpolation\n- Model Chaining\n"
            //+ "- Custom Tile Size (Uses Automatic Tile Size)", "Warning");
        }
    }
}