using System.Diagnostics;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.Main;
using Cupscale.UI;

namespace Cupscale.ImageUtils
{
    internal class NvCompress
    {
        private static Process currentProcess;

        public static async Task SaveDds(string inputFile, string outputPath)
        {
            var dxtString = Config.Get("dxtMode").ToLower().Replace("off", "rgb");
            if (dxtString.Contains(" "))
                dxtString = dxtString.Split(' ')[0];
            await Run(inputFile, outputPath, dxtString, Config.GetBool("alpha"), Config.GetBool("ddsEnableMips"));
        }

        public static async Task Run(string inpath, string outpath, string dxtMode, bool alpha, bool enableMips)
        {
            inpath = inpath.WrapPath(true, true);
            outpath = outpath.WrapPath(true);

            var alphaStr = "";
            if (alpha)
                alphaStr = " -alpha ";

            var mipStr = " -nomips ";
            if (enableMips)
                mipStr = "";

            var cmd2 = "/C cd /D " + Config.Get("esrganPath").WrapPath()
                                   + " & nvcompress.exe -" + dxtMode + alphaStr + mipStr + inpath + outpath;
            Logger.Log("CMD: " + cmd2);
            var nvCompress = new Process();
            nvCompress.StartInfo.UseShellExecute = false;
            nvCompress.StartInfo.RedirectStandardOutput = true;
            nvCompress.StartInfo.RedirectStandardError = true;
            nvCompress.StartInfo.CreateNoWindow = true;
            nvCompress.StartInfo.FileName = "cmd.exe";
            nvCompress.StartInfo.Arguments = cmd2;
            nvCompress.OutputDataReceived += OutputHandler;
            nvCompress.ErrorDataReceived += OutputHandler;
            currentProcess = nvCompress;
            nvCompress.Start();
            nvCompress.BeginOutputReadLine();
            nvCompress.BeginErrorReadLine();
            while (!nvCompress.HasExited)
                await Task.Delay(50);
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs output)
        {
            if (output == null || output.Data == null)
                return;

            var data = output.Data;

            if (data.Length >= 5)
                Logger.Log("[NVCOMPRESS] " + data.Replace("\n", " ").Replace("\r", " "));
        }
    }
}