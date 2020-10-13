using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.Forms;
using Cupscale.Properties;
using Cupscale.UI;
using SevenZipNET;

namespace Cupscale.IO
{
    internal class ShippedEsrgan
    {
        public static string path;

        private static string path7za = "";

        public static async Task Init()
        {
            Logger.Log("ShippedEsrgan Init()");
            path = Paths.esrganPath;
            if (!InstallationIsValid())
                await Install();
            else
                Logger.Log("Installation valid.");
        }

        public static bool InstallationIsValid()
        {
            var requiredDirs = new List<string>();
            requiredDirs.Add(Path.Combine(path, "Tools"));
            requiredDirs.Add(Path.Combine(path, "Tools", "Lib", "site-packages"));
            requiredDirs.Add(Path.Combine(path, "utils"));

            var requiredFiles = new List<string>();
            requiredFiles.Add(Path.Combine(IOUtils.GetAppDataDir(), "shipped_files_version"));
            requiredFiles.Add(Path.Combine(path, "esrlmain.py"));
            requiredFiles.Add(Path.Combine(path, "esrlupscale.py"));
            requiredFiles.Add(Path.Combine(path, "esrlmodel.py"));
            requiredFiles.Add(Path.Combine(path, "esrlrrdbnet.py"));
            requiredFiles.Add(Path.Combine(path, "ffmpeg.exe"));
            requiredFiles.Add(Path.Combine(path, "esrgan-ncnn-vulkan.exe"));
            requiredFiles.Add(Path.Combine(path, "pth2ncnn.exe"));
            requiredFiles.Add(Path.Combine(path, "nvcompress.exe"));
            requiredFiles.Add(Path.Combine(path, "nvtt.dll"));
            requiredFiles.Add(Path.Combine(path, "gifski.exe"));

            foreach (var dir in requiredDirs)
                if (!Directory.Exists(dir))
                {
                    Logger.Log("Installation invalid: Directory " + dir + " not found");
                    return false;
                }

            foreach (var file in requiredFiles)
                if (!File.Exists(file))
                {
                    Logger.Log("Installation invalid: File " + file + " not found");
                    return false;
                }

            var exeVersion = new StringReader(Resources.shipped_files_version).ReadLine().GetInt();
            var diskVersion = IOUtils.ReadLines(Path.Combine(IOUtils.GetAppDataDir(), "shipped_files_version"))[0]
                .GetInt();
            if (exeVersion != diskVersion)
            {
                Logger.Log("Installation invalid: Shipped file version mismatch - Executable is " + exeVersion +
                           ", installation is " + diskVersion);
                return false;
            }

            return true;
        }

        public static async Task Install()
        {
            Program.mainForm.Enabled = false;
            var dialog = new DialogForm("Installing resources...\nThis only needs to be done once.");
            await Task.Delay(20);
            Directory.CreateDirectory(path);

            path7za = Path.Combine(path, "7za.exe");
            File.WriteAllBytes(path7za, Resources.x64_7za);
            File.WriteAllBytes(Path.Combine(path, "nvcompress.exe"), Resources.nvcompress);
            File.WriteAllBytes(Path.Combine(path, "nvtt.dll"), Resources.nvtt);
            File.WriteAllBytes(Path.Combine(IOUtils.GetAppDataDir(), "esrgan.7z"), Resources.esrgan);
            File.WriteAllBytes(Path.Combine(IOUtils.GetAppDataDir(), "ncnn.7z"), Resources.esrgan_ncnn);
            File.WriteAllBytes(Path.Combine(IOUtils.GetAppDataDir(), "ffmpeg.7z"), Resources.ffmpeg);

            dialog.ChangeText("Installing ESRGAN resources...");
            await UnSevenzip(Path.Combine(IOUtils.GetAppDataDir(), "esrgan.7z"));
            dialog.ChangeText("Installing ESRGAN-NCNN resources...");
            await UnSevenzip(Path.Combine(IOUtils.GetAppDataDir(), "ncnn.7z"));
            dialog.ChangeText("Installing FFmpeg resources...");
            await UnSevenzip(Path.Combine(IOUtils.GetAppDataDir(), "ffmpeg.7z"));

            File.WriteAllText(Path.Combine(IOUtils.GetAppDataDir(), "shipped_files_version"),
                Resources.shipped_files_version);

            dialog.Close();
            Program.mainForm.Enabled = true;
            Program.mainForm.WindowState = FormWindowState.Maximized;
            Program.mainForm.BringToFront();
        }

        private static async Task UnSevenzip(string path)
        {
            Logger.Log("Extracting " + path);
            await Task.Delay(20);
            SevenZipBase.Path7za = path7za;
            var extractor = new SevenZipExtractor(path);
            extractor.ExtractAll(IOUtils.GetAppDataDir(), true);
            File.Delete(path);
            await Task.Delay(10);
        }

        public static bool Exists()
        {
            var directoryInfo = new DirectoryInfo(path);
            if (directoryInfo == null || !Directory.Exists(directoryInfo.FullName)) return false;
            var files = directoryInfo.GetFiles("*.py", SearchOption.AllDirectories);
            if (files.Length >= 4) return true;
            return false;
        }

        public static void Uninstall(bool full)
        {
            if (full)
                Directory.Delete(IOUtils.GetAppDataDir(), true);
            else
                Directory.Delete(path, true);
        }
    }
}