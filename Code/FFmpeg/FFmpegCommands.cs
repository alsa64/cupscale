using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.Main;
using Cupscale.UI;

namespace Cupscale.FFmpeg
{
    internal class FFmpegCommands
    {
        public static async Task VideoToFrames(string inputFile, string frameFolderPath, bool deDupe, bool hdr,
            bool delSrc)
        {
            if (!Directory.Exists(frameFolderPath))
                Directory.CreateDirectory(frameFolderPath);
            var hdrStr = "";
            if (hdr) hdrStr = FFmpegStrings.hdrFilter;
            var deDupeStr = "";
            if (deDupe) deDupeStr = "-vf mpdecimate";
            var args = "-i \"" + inputFile + "\" " + hdrStr + " -vsync 0 " + deDupeStr + " \"" + frameFolderPath +
                       "/%04d.png\"";
            await FFmpeg.Run(args);
            await Task.Delay(1);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async void ExtractSingleFrame(string inputFile, int frameNum, bool hdr, bool delSrc)
        {
            var hdrStr = "";
            if (hdr) hdrStr = FFmpegStrings.hdrFilter;
            var args = "-i " + inputFile.WrapPath() + " " + hdrStr
                       + " -vf \"select=eq(n\\," + frameNum + ")\" -vframes 1  " + inputFile.WrapPath() + "-frame" +
                       frameNum + ".png";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task FramesToMp4(string inputDir, bool useH265, int crf, int fps, string prefix,
            bool delSrc)
        {
            var nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            var enc = "libx264";
            if (useH265) enc = "libx265";
            var args = " -framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums + "d.png\" -c:v " + enc
                       + " -crf " + crf +
                       " -pix_fmt yuv420p -movflags +faststart -vf \"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"  -c:a copy \"" +
                       inputDir + ".mp4\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputDir); // CHANGE CODE TO BE ABLE TO DELETE DIRECTORIES!!
        }

        public static async Task FramesToOneFpsMp4(string inputDir, bool useH265, int crf, int loopTimes, string prefix,
            bool delSrc)
        {
            var nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            var enc = "libx264";
            if (useH265) enc = "libx265";
            var args = " -framerate 1 -stream_loop " + loopTimes + " -i \"" + inputDir + "\\" + prefix + "%0" + nums +
                       "d.png\" -c:v " + enc + " -r 30"
                       + " -crf " + crf +
                       " -pix_fmt yuv420p -movflags +faststart -vf \"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"  -c:a copy \"" +
                       inputDir + ".mp4\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputDir); // CHANGE CODE TO BE ABLE TO DELETE DIRECTORIES!!
        }

        public static async Task FramesToMp4Looped(string inputDir, bool useH265, int crf, int fps, int loopTimes,
            string prefix, bool delSrc)
        {
            var nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            var enc = "libx264";
            if (useH265) enc = "libx265";
            var args = " -framerate " + fps + " -stream_loop " + loopTimes + " -i \"" + inputDir + "\\" + prefix +
                       "%0" + nums + "d.png\" -c:v " + enc
                       + " -crf " + crf +
                       " -pix_fmt yuv420p -movflags +faststart -vf \"crop = trunc(iw / 2) * 2:trunc(ih / 2) * 2\"  -c:a copy \"" +
                       inputDir + ".mp4\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputDir); // CHANGE CODE TO BE ABLE TO DELETE DIRECTORIES!!
        }

        public static async void FramesToApng(string inputDir, bool opti, int fps, string prefix, bool delSrc)
        {
            var nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            var filter = "";
            if (opti) filter = "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"";
            var args = "-framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums +
                       "d.png\" -f apng -plays 0 " + filter + " \"" + inputDir + "-anim.png\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputDir); // CHANGE CODE TO BE ABLE TO DELETE DIRECTORIES!!
        }

        public static async Task FramesToGif(string inputDir, bool opti, int fps, string prefix, bool delSrc)
        {
            var nums = IOUtils.GetFilenameCounterLength(Directory.GetFiles(inputDir, "*.png")[0], prefix);
            var filter = "";
            if (opti) filter = "-vf \"split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\"";
            var args = "-framerate " + fps + " -i \"" + inputDir + "\\" + prefix + "%0" + nums + "d.png\" -f gif " +
                       filter + " \"" + inputDir + ".gif\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputDir); // CHANGE CODE TO BE ABLE TO DELETE DIRECTORIES!!
        }

        public static async Task LoopVideo(string inputFile, int times, bool delSrc)
        {
            var pathNoExt = Path.ChangeExtension(inputFile, null);
            var ext = Path.GetExtension(inputFile);
            var args = " -stream_loop " + times + " -i \"" + inputFile + "\"  -c copy \"" + pathNoExt + "-" + times +
                       "xLoop" + ext + "\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task LoopVideoEnc(string inputFile, int times, bool useH265, int crf, bool delSrc)
        {
            var pathNoExt = Path.ChangeExtension(inputFile, null);
            var ext = Path.GetExtension(inputFile);
            var enc = "libx264";
            if (useH265) enc = "libx265";
            var args = " -stream_loop " + times + " -i \"" + inputFile + "\"  -c:v " + enc + " -crf " + crf +
                       " -c:a copy \"" + pathNoExt + "-" + times + "xLoop" + ext + "\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task ChangeSpeed(string inputFile, float newSpeedPercent, bool delSrc)
        {
            var pathNoExt = Path.ChangeExtension(inputFile, null);
            var ext = Path.GetExtension(inputFile);
            var val = newSpeedPercent / 100f;
            var speedVal = (1f / val).ToString("0.0000").Replace(",", ".");
            var args = " -itsscale " + speedVal + " -i \"" + inputFile + "\"  -c copy \"" + pathNoExt + "-" +
                       newSpeedPercent + "pcSpeed" + ext + "\"";
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static async Task Encode(string inputFile, string vcodec, string acodec, int crf, int audioKbps,
            bool delSrc)
        {
            var args = " -i \"INPATH\" -c:v VCODEC -crf CRF -pix_fmt yuv420p -c:a ACODEC -b:a ABITRATE \"OUTPATH\"";
            if (string.IsNullOrWhiteSpace(acodec))
                args = args.Replace("-c:a", "-an");
            args = args.Replace("VCODEC", vcodec);
            args = args.Replace("ACODEC", acodec);
            args = args.Replace("CRF", crf.ToString());
            if (audioKbps > 0)
                args = args.Replace("ABITRATE", audioKbps.ToString());
            else
                args = args.Replace(" -b:a ABITRATE", "");
            var filenameNoExt = Path.ChangeExtension(inputFile, null);
            args = args.Replace("INPATH", inputFile);
            args = args.Replace("OUTPATH", filenameNoExt + "-convert.mp4");
            await FFmpeg.Run(args);
            if (delSrc)
                DeleteSource(inputFile);
        }

        public static float GetFramerate(string inputFile)
        {
            var args = " -i \"INPATH\"";
            args = args.Replace("INPATH", inputFile);
            var ffmpegOut = FFmpeg.RunAndGetOutput(args);
            var entries = ffmpegOut.Split(',');
            foreach (var entry in entries)
                if (entry.Contains(" fps"))
                {
                    var num = entry.Replace(" fps", "").Trim().Replace(",", ".");
                    float value;
                    float.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
                    return value;
                }

            return 0f;
        }

        private static void DeleteSource(string path)
        {
            Logger.Log("Deleting input file: " + path);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}