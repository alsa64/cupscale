using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.UI;
using ImageMagick;
using Paths = Cupscale.IO.Paths;

namespace Cupscale
{
    internal static class Program
    {
        public static MainForm mainForm;
        public static string lastOutputDir;
        public static string lastFilename;
        public static string lastDirPath;
        public static string lastModelName;
        public static string currentModel1;
        public static string currentModel2;
        public static FilterType currentFilter = FilterType.Point;

        public static List<Form>
            currentTemporaryForms = new List<Form>(); // Temp forms that get closed when something gets cancelled

        public static bool busy;

        [STAThread]
        private static void Main()
        {
            Application.SetCompatibleTextRenderingDefault(false);
            Application.EnableVisualStyles();
            Console.WriteLine("Main()");
            IOUtils.DeleteIfExists(Path.Combine(IOUtils.GetAppDataDir(), "sessionlog.txt"));
            Config.Init();
            Paths.Init();
            EsrganData.CheckModelDir();
            ResourceLimits.Memory = (ulong) Math.Round(ResourceLimits.Memory * 1.5f);
            Cleanup();
            Application.Run(new MainForm());
        }

        public static void Cleanup()
        {
            IOUtils.DeleteContentsOfDir(Paths.previewPath);
            IOUtils.DeleteContentsOfDir(Paths.previewPath);
            IOUtils.DeleteContentsOfDir(Paths.clipboardFolderPath);
            IOUtils.DeleteContentsOfDir(Paths.imgInPath);
            IOUtils.DeleteContentsOfDir(Paths.imgOutPath);
            IOUtils.DeleteContentsOfDir(Paths.imgOutNcnnPath);
            IOUtils.DeleteContentsOfDir(Paths.tempImgPath.GetParentDir());
            IOUtils.DeleteContentsOfDir(Path.Combine(IOUtils.GetAppDataDir(), "giftemp"));
            IOUtils.DeleteIfExists(Path.Combine(Paths.presetsPath, "lastUsed"));
        }

        public static void CloseTempForms()
        {
            foreach (var form in currentTemporaryForms)
                form.Close();
        }

        public static async Task PutTaskDelay()
        {
            await Task.Delay(1);
        }

        public static int GetPercentage(float val1, float val2)
        {
            return (int) Math.Round(val1 / val2 * 100f);
        }

        public static void Quit()
        {
            Application.Exit();
        }
    }
}