using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.UI;

namespace Cupscale.Main
{
    internal class PostProcessingQueue
    {
        //public static bool ncnn;

        public enum CopyMode
        {
            KeepStructure,
            CopyToRoot
        }

        public static Queue<string> outputFileQueue = new Queue<string>();
        public static List<string> processedFiles = new List<string>();
        public static List<string> outputFiles = new List<string>();

        public static bool run;
        public static string currentOutPath;
        public static CopyMode copyMode;

        public static string lastOutfile;

        public static void Start(string outpath)
        {
            currentOutPath = outpath;
            outputFileQueue.Clear();
            processedFiles.Clear();
            outputFiles.Clear();
            IOUtils.DeleteContentsOfDir(Paths.imgOutNcnnPath);
            run = true;
        }

        public static void Stop()
        {
            Logger.Log("PostProcessingQueue.Stop()");
            run = false;
        }

        public static async Task Update()
        {
            while (run || AnyFilesLeft())
            {
                var outFiles = Directory.GetFiles(Paths.imgOutPath, "*.png.*", SearchOption.AllDirectories);
                Logger.Log("Queue Update() - " + outFiles.Length + " files in out folder");
                foreach (var file in outFiles)
                    if (!outputFileQueue.Contains(file) && !processedFiles.Contains(file) &&
                        !outputFiles.Contains(file))
                    {
                        //processedFiles.Add(file);
                        outputFileQueue.Enqueue(file);
                        Logger.Log("[Queue] Enqueued " + Path.GetFileName(file));
                    }
                    else
                    {
                        Logger.Log("Skipped " + file + " - Is In Queue: " + outputFileQueue.Contains(file) +
                                   " - Is Processed: " + processedFiles.Contains(file) + " - Is Outfile: " +
                                   outputFiles.Contains(file));
                    }

                await Task.Delay(1000);
            }
        }

        private static bool AnyFilesLeft()
        {
            if (IOUtils.GetAmountOfFiles(Paths.imgOutPath, true) > 0)
                return true;
            Logger.Log("No files in Paths.imgOutPath");
            return false;
        }

        public static async Task ProcessQueue()
        {
            var sw = new Stopwatch();
            Logger.Log("ProcessQueue()");
            while (run || AnyFilesLeft())
            {
                if (outputFileQueue.Count > 0)
                {
                    var file = outputFileQueue.Dequeue();
                    Logger.Log("[Queue] Post-Processing " + Path.GetFileName(file));
                    sw.Restart();
                    await Upscale.PostprocessingSingle(file, true);
                    var outFilename = Upscale.FilenamePostprocess(lastOutfile);
                    outputFiles.Add(outFilename);
                    Logger.Log("[Queue] Done Post-Processing " + Path.GetFileName(file) + " in " +
                               sw.ElapsedMilliseconds + "ms");

                    if (Upscale.overwriteMode == Upscale.Overwrite.Yes)
                    {
                        var suffixToRemove = "-" + Program.lastModelName.Replace(":", ".").Replace(">>", "+");
                        if (copyMode == CopyMode.KeepStructure)
                        {
                            var combinedPath = currentOutPath + outFilename.Replace(Paths.imgOutPath, "");
                            Directory.CreateDirectory(combinedPath.GetParentDir());
                            File.Copy(outFilename, combinedPath.ReplaceInFilename(suffixToRemove, ""));
                        }

                        if (copyMode == CopyMode.CopyToRoot)
                            File.Copy(outFilename,
                                Path.Combine(currentOutPath, Path.GetFileName(outFilename).Replace(suffixToRemove, "")),
                                true);
                        File.Delete(outFilename);
                    }
                    else
                    {
                        if (copyMode == CopyMode.KeepStructure)
                        {
                            var combinedPath = currentOutPath + outFilename.Replace(Paths.imgOutPath, "");
                            Directory.CreateDirectory(combinedPath.GetParentDir());
                            File.Copy(outFilename, combinedPath, true);
                        }

                        if (copyMode == CopyMode.CopyToRoot)
                            File.Copy(outFilename, Path.Combine(currentOutPath, Path.GetFileName(outFilename)), true);
                        File.Delete(outFilename);
                    }

                    BatchUpscaleUi.upscaledImages++;
                }

                await Task.Delay(250);
            }
        }
    }
}