using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.Main;
using NvAPIWrapper;
using NvAPIWrapper.GPU;

namespace Cupscale.OS
{
    internal class NvApi
    {
        private static PhysicalGPU gpu;
        private static float vramGb;
        private static float vramFreeGb;

        public static void Init()
        {
            try
            {
                NVIDIA.Initialize();
                var gpus = PhysicalGPU.GetPhysicalGPUs();
                if (gpus.Length == 0)
                    return;
                gpu = gpus[0];

                RefreshVram();
                RefreshLoop();
            }
            catch (Exception e)
            {
                Logger.Log("Failed to initialize NvApi: " + e.Message);
            }
        }

        public static void RefreshVram()
        {
            if (Form.ActiveForm != Program.mainForm || gpu == null) // Don't refresh if not in focus or no GPU detected
                return;
            vramGb = gpu.MemoryInformation.AvailableDedicatedVideoMemoryInkB / 1000f / 1024f;
            vramFreeGb = gpu.MemoryInformation.CurrentAvailableDedicatedVideoMemoryInkB / 1000f / 1024f;
            var col = Color.White;
            if (vramFreeGb < 2f)
                col = Color.Orange;
            if (vramFreeGb < 1f)
                col = Color.OrangeRed;
            Program.mainForm.SetVramLabel(
                $"{gpu.FullName}: {vramGb.ToString("0.00")} GB VRAM - {vramFreeGb.ToString("0.00")} GB Free", col);
        }

        public static async void RefreshLoop()
        {
            RefreshVram();
            await Task.Delay(1000);
            RefreshLoop();
        }
    }
}