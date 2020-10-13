using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.UI;

namespace Cupscale.OS
{
    internal class NcnnUtils
    {
        private static Process currentProcess;
        private static string ncnnDir = "";

        public static async Task ConvertNcnnModel(string modelPath)
        {
            var modelName = Path.GetFileName(modelPath);
            ncnnDir = Path.Combine(Config.Get("modelPath"), ".ncnn");
            Directory.CreateDirectory(ncnnDir);
            var outPath = Path.Combine(ncnnDir, Path.ChangeExtension(modelName, null));
            Logger.Log("Checking for NCNN model: " + outPath);
            if (IOUtils.GetAmountOfFiles(outPath, false) < 2)
            {
                Logger.Log("Running model converter...");
                await RunConverter(modelPath);
                var moveFrom = Path.Combine(Config.Get("esrganPath"), Path.ChangeExtension(modelName, null));
                Logger.Log("Moving " + moveFrom + " to " + outPath);
                IOUtils.Copy(moveFrom, outPath, "*", true);
                Directory.Delete(moveFrom, true);
            }
            else
            {
                Logger.Log("NCNN Model is cached - Skipping conversion.");
            }

            ESRGAN.currentNcnnModel = outPath;
        }

        private static async Task RunConverter(string modelPath)
        {
            modelPath = modelPath.WrapPath();

            var cmd2 = "/C cd /D " + Config.Get("esrganPath").WrapPath() + " & pth2ncnn.exe " + modelPath;

            Logger.Log("CMD: " + cmd2);
            var converterProc = new Process();
            //converterProc.StartInfo.UseShellExecute = false;
            //converterProc.StartInfo.RedirectStandardOutput = true;
            //converterProc.StartInfo.RedirectStandardError = true;
            //converterProc.StartInfo.CreateNoWindow = true;
            converterProc.StartInfo.FileName = "cmd.exe";
            converterProc.StartInfo.Arguments = cmd2;
            converterProc.OutputDataReceived += OutputHandler;
            converterProc.ErrorDataReceived += OutputHandler;
            currentProcess = converterProc;
            converterProc.Start();
            //converterProc.BeginOutputReadLine();
            //converterProc.BeginErrorReadLine();
            while (!converterProc.HasExited) await Task.Delay(100);
            File.Delete(Paths.progressLogfile);
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs output)
        {
            if (output == null || output.Data == null)
                return;

            var data = output.Data;
            Logger.Log("Model Converter Output: " + data);
        }

        public static int GetNcnnModelScale(string modelDir)
        {
            var files = Directory.GetFiles(modelDir, "*.bin");
            return Path.GetFileNameWithoutExtension(files[0]).GetInt();
        }
    }
}