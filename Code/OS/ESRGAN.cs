using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.Forms;
using Cupscale.ImageUtils;
using Cupscale.IO;
using Cupscale.Main;
using Cupscale.Preview;
using Cupscale.UI;

namespace Cupscale.OS
{
    internal class ESRGAN
    {
        public enum Backend
        {
            CUDA,
            CPU,
            NCNN
        }

        public enum PreviewMode
        {
            None,
            Cutout,
            FullImage
        }

        private static Process currentProcess;

        private static string lastProgressString = "";

        public static string currentNcnnModel = "";


        public static async Task DoUpscale(string inpath, string outpath, ModelData mdl, string tilesize, bool alpha,
            PreviewMode mode, Backend backend, bool showTileProgress = true)
        {
            var useJoey = Config.GetInt("esrganVersion") == 1;
            try
            {
                if (backend == Backend.NCNN)
                {
                    Program.mainForm.SetProgress(1f, "Loading ESRGAN-NCNN...");
                    var dialogForm = new DialogForm("Loading ESRGAN-NCNN...\nThis should take 10-25 seconds.", 14);
                    Program.lastModelName = mdl.model1Name;
                    //PostProcessingQueue.ncnn = true;
                    await RunNcnn(inpath, outpath, mdl.model1Path);
                }
                else
                {
                    Program.mainForm.SetProgress(5f, "Starting ESRGAN...");
                    File.Delete(Paths.progressLogfile);
                    var modelArg = GetModelArg(mdl, useJoey);
                    Logger.Log("Model Arg: " + modelArg);
                    //PostProcessingQueue.ncnn = false;
                    if (useJoey)
                        await RunJoey(inpath, outpath, modelArg, tilesize, alpha, showTileProgress);
                    else
                        await Run(inpath, outpath, modelArg, tilesize, alpha, showTileProgress);
                }

                if (mode == PreviewMode.Cutout)
                {
                    await ScalePreviewOutput();
                    Program.mainForm.SetProgress(100f, "Merging into preview...");
                    await Program.PutTaskDelay();
                    PreviewMerger.Merge();
                    Program.mainForm.SetHasPreview(true);
                }

                if (mode == PreviewMode.FullImage)
                {
                    await ScalePreviewOutput();
                    Program.mainForm.SetProgress(100f, "Merging into preview...");
                    await Program.PutTaskDelay();
                    var outImg =
                        ImgUtils.GetImage(Directory.GetFiles(Paths.previewOutPath, "*.png.*",
                            SearchOption.AllDirectories)[0]);
                    var inputImg = ImgUtils.GetImage(Paths.tempImgPath);
                    MainUiHelper.previewImg.Image = outImg;
                    MainUiHelper.currentOriginal = inputImg;
                    MainUiHelper.currentOutput = outImg;
                    MainUiHelper.currentScale = ImgUtils.GetScaleFloat(inputImg, outImg);
                    MainUiHelper.previewImg.ZoomToFit();
                    Program.mainForm.SetHasPreview(true);
                    Program.mainForm.SetProgress(0f, "Done.");
                }
            }
            catch (Exception e)
            {
                if (e.Message.Contains("No such file"))
                    MessageBox.Show(
                        "An error occured during upscaling.\nThe upscale process seems to have exited before completion!",
                        "Error");
                else
                    MessageBox.Show("An error occured during upscaling.", "Error");
                Logger.Log("Upscaling Error: " + e.Message + "\n" + e.StackTrace);
                Program.mainForm.SetProgress(0f, "Cancelled.");
            }
        }

        public static async Task ScalePreviewOutput()
        {
            if (ImageProcessing.postScaleMode == Upscale.ScaleMode.Percent && ImageProcessing.postScaleValue == 100
            ) // Skip if target scale is 100%)
                return;
            Program.mainForm.SetProgress(1f, "Resizing preview output...");
            await Task.Delay(1);
            var img = ImgUtils.GetMagickImage(Directory.GetFiles(Paths.previewOutPath, "*.png.*",
                SearchOption.AllDirectories)[0]);
            var magickImage = ImageProcessing.ResizeImagePost(img);
            img = magickImage;
            img.Write(img.FileName);
        }

        public static string GetModelArg(ModelData mdl, bool joey)
        {
            var mdl1 = mdl.model1Path;
            var mdl2 = mdl.model2Path;
            var mdlMode = mdl.mode;
            if (mdlMode == ModelData.ModelMode.Single)
            {
                Program.lastModelName = mdl.model1Name;
                if (joey)
                    return mdl1.WrapPath(true);
                return " --model \"" + mdl1 + "\"";
            }

            if (mdlMode == ModelData.ModelMode.Interp)
            {
                var interpLeft = 100 - mdl.interp;
                var interpRight = mdl.interp;
                Program.lastModelName = mdl.model1Name + ":" + interpLeft + ":" + mdl.model2Name + ":" + interpRight;
                if (joey)
                    return (mdl1 + ";" + interpLeft + "&" + mdl2 + ";" + interpRight).WrapPath(true);
                return " --model " + mdl1.WrapPath() + ";" + interpLeft + ";" + mdl2.WrapPath() + ";" + interpRight;
            }

            if (mdlMode == ModelData.ModelMode.Chain)
            {
                Program.lastModelName = mdl.model1Name + ">>" + mdl.model2Name;
                if (joey)
                    return (mdl1 + ">" + mdl2).WrapPath(true);
                return " --model  " + mdl1.WrapPath() + " --postfilter " + mdl2.WrapPath();
            }

            if (mdlMode == ModelData.ModelMode.Advanced)
            {
                Program.lastModelName = "Advanced";
                return AdvancedModelSelection.GetArg(joey);
            }

            return null;
        }

        public static async Task Run(string inpath, string outpath, string modelArg, string tilesize, bool alpha,
            bool showTileProgress)
        {
            inpath = inpath.WrapPath();
            outpath = outpath.WrapPath();
            var alphaStr = " --noalpha";
            if (alpha) alphaStr = "";
            var deviceStr = " --device cuda";
            if (Config.Get("cudaFallback").GetInt() == 1 || Config.Get("cudaFallback").GetInt() == 2)
                deviceStr = " --device cpu";
            var cmd2 = "/C cd /D \"" + Config.Get("esrganPath") + "\" & ";
            cmd2 = cmd2 + "python esrlmain.py " + inpath + " " + outpath + deviceStr + " --tilesize " + tilesize +
                   alphaStr + modelArg;
            Logger.Log("CMD: " + cmd2);
            var esrganProcess = new Process();
            esrganProcess.StartInfo.UseShellExecute = false;
            esrganProcess.StartInfo.RedirectStandardOutput = true;
            esrganProcess.StartInfo.RedirectStandardError = true;
            esrganProcess.StartInfo.CreateNoWindow = true;
            esrganProcess.StartInfo.FileName = "cmd.exe";
            esrganProcess.StartInfo.Arguments = cmd2;
            esrganProcess.OutputDataReceived += OutputHandler;
            esrganProcess.ErrorDataReceived += OutputHandler;
            currentProcess = esrganProcess;
            esrganProcess.Start();
            esrganProcess.BeginOutputReadLine();
            esrganProcess.BeginErrorReadLine();
            while (!esrganProcess.HasExited)
            {
                if (showTileProgress)
                    await UpdateProgressFromFile();
                await Task.Delay(100);
            }

            if (Upscale.currentMode == Upscale.UpscaleMode.Batch)
            {
                await Task.Delay(1000);
                Program.mainForm.SetProgress(100f, "Post-Processing...");
                PostProcessingQueue.Stop();
            }

            File.Delete(Paths.progressLogfile);
        }

        public static async Task RunJoey(string inpath, string outpath, string modelArg, string tilesize, bool alpha,
            bool showTileProgress)
        {
            inpath = inpath.WrapPath(true, true);
            outpath = outpath.WrapPath(true, true);

            var alphaStr = "";
            if (alpha) alphaStr = " --alpha_mode 2 ";

            var deviceStr = "";
            if (Config.Get("cudaFallback").GetInt() == 1 || Config.Get("cudaFallback").GetInt() == 2)
                deviceStr = " --cpu ";

            var seamStr = "";
            if (Config.Get("seamlessMode").GetInt() == 1) seamStr = " --seamless";
            if (Config.Get("seamlessMode").GetInt() == 2) seamStr = " --mirror";

            var cmd = "/C cd /D " + Config.Get("esrganPath").WrapPath() + " & python upscale.py --input" + inpath +
                      "--output" + outpath
                      + deviceStr + seamStr + " --tile_size " + tilesize + alphaStr + modelArg;
            Logger.Log("CMD: " + cmd);
            var esrganProcess = new Process();
            esrganProcess.StartInfo.UseShellExecute = false;
            esrganProcess.StartInfo.RedirectStandardOutput = true;
            esrganProcess.StartInfo.RedirectStandardError = true;
            esrganProcess.StartInfo.CreateNoWindow = true;
            esrganProcess.StartInfo.FileName = "cmd.exe";
            esrganProcess.StartInfo.Arguments = cmd;
            esrganProcess.OutputDataReceived += OutputHandler;
            esrganProcess.ErrorDataReceived += OutputHandler;
            currentProcess = esrganProcess;
            esrganProcess.Start();
            esrganProcess.BeginOutputReadLine();
            esrganProcess.BeginErrorReadLine();
            while (!esrganProcess.HasExited)
            {
                if (showTileProgress)
                    await UpdateProgressFromFile();
                await Task.Delay(100);
            }

            if (Upscale.currentMode == Upscale.UpscaleMode.Batch)
            {
                await Task.Delay(1000);
                Program.mainForm.SetProgress(100f, "Post-Processing...");
                PostProcessingQueue.Stop();
            }

            File.Delete(Paths.progressLogfile);
        }

        private static void OutputHandler(object sendingProcess, DataReceivedEventArgs output)
        {
            if (output == null || output.Data == null) return;
            var data = output.Data;
            Logger.Log("[ESRGAN] " + data.Replace("\n", " ").Replace("\r", " "));
            if (data.Contains("RuntimeError"))
            {
                if (currentProcess != null && !currentProcess.HasExited) currentProcess.Kill();
                MessageBox.Show("Error occurred: \n\n" + data + "\n\nThe ESRGAN process was killed to avoid lock-ups.",
                    "Error");
            }

            if (data.Contains("out of memory"))
                MessageBox.Show(
                    "ESRGAN ran out of memory. Try reducing the tile size and avoid running programs in the background (especially games) that take up your VRAM.",
                    "Error");

            if (data.Contains("Python was not found"))
                MessageBox.Show("Python was not found. Make sure you have a working Python 3 installation.", "Error");

            if (data.Contains("ModuleNotFoundError"))
                MessageBox.Show(
                    "You are missing ESRGAN Python dependencies. Make sure Pytorch, cv2 (opencv-python) and tensorboardx are installed.",
                    "Error");

            if (data.Contains("RRDBNet"))
                MessageBox.Show("Model appears to be incompatible!", "Error");

            if (data.Contains("UnpicklingError"))
                MessageBox.Show("Failed to load model!", "Error");
        }

        private static async Task UpdateProgressFromFile()
        {
            var progressLogFile = Paths.progressLogfile;
            if (!File.Exists(progressLogFile))
                return;
            var lines = IOUtils.ReadLines(progressLogFile);
            if (lines.Length < 1)
                return;
            var outStr = lines[lines.Length - 1];
            if (outStr == lastProgressString)
                return;
            lastProgressString = outStr;
            var text = outStr.Replace("Tile ", "").Trim();
            try
            {
                var num = int.Parse(text.Split('/')[0]);
                var num2 = int.Parse(text.Split('/')[1]);
                var previewProgress = num / (float) num2 * 100f;
                Program.mainForm.SetProgress(previewProgress,
                    "Processing Tiles - " + previewProgress.ToString("0") + "%");
            }
            catch
            {
                Logger.Log("Failed to parse progress from this line: " + text);
            }

            await Task.Delay(1);
        }

        public static async Task RunNcnn(string inpath, string outpath, string modelPath)
        {
            inpath = inpath.WrapPath();
            outpath = outpath.WrapPath();

            Program.mainForm.SetProgress(3f, "Converting NCNN model...");
            await NcnnUtils.ConvertNcnnModel(modelPath);
            Logger.Log("NCNN Model is ready: " + currentNcnnModel);
            Program.mainForm.SetProgress(5f, "Loading ESRGAN-NCNN...");
            var scale = NcnnUtils.GetNcnnModelScale(currentNcnnModel);
            var cmd2 = "/C cd /D \"" + Config.Get("esrganPath") + "\" & "
                       + "esrgan-ncnn-vulkan.exe -i " + inpath + " -o " + outpath + " -m " +
                       currentNcnnModel.WrapPath() + " -s " + scale;
            Logger.Log("CMD: " + cmd2);
            var ncnnProcess = new Process();
            ncnnProcess.StartInfo.UseShellExecute = false;
            ncnnProcess.StartInfo.RedirectStandardOutput = true;
            ncnnProcess.StartInfo.RedirectStandardError = true;
            ncnnProcess.StartInfo.CreateNoWindow = true;
            ncnnProcess.StartInfo.FileName = "cmd.exe";
            ncnnProcess.StartInfo.Arguments = cmd2;
            ncnnProcess.OutputDataReceived += NcnnOutputHandler;
            ncnnProcess.ErrorDataReceived += NcnnOutputHandler;
            currentProcess = ncnnProcess;
            ncnnProcess.Start();
            ncnnProcess.BeginOutputReadLine();
            ncnnProcess.BeginErrorReadLine();
            while (!ncnnProcess.HasExited) await Task.Delay(100);
            if (Upscale.currentMode == Upscale.UpscaleMode.Batch)
            {
                await Task.Delay(1000);
                Program.mainForm.SetProgress(100f, "Post-Processing...");
                PostProcessingQueue.Stop();
            }

            File.Delete(Paths.progressLogfile);
        }

        private static void NcnnOutputHandler(object sendingProcess, DataReceivedEventArgs output)
        {
            if (output == null || output.Data == null)
                return;

            var data = output.Data;
            Logger.Log("[NCNN] " + data.Replace("\n", " ").Replace("\r", " "));
            if (data.Contains("failed"))
            {
                if (currentProcess != null && !currentProcess.HasExited)
                    currentProcess.Kill();

                MessageBox.Show(
                    "Error occurred: \n\n" + data + "\n\nThe ESRGAN-NCNN process was killed to avoid lock-ups.",
                    "Error");
            }

            if (data.Contains("vkAllocateMemory"))
                MessageBox.Show(
                    "ESRGAN-NCNN ran out of memory. Try reducing the tile size and avoid running programs in the background (especially games) that take up your VRAM.",
                    "Error");
        }
    }
}