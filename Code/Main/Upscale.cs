using System;
using System.IO;
using System.Threading.Tasks;
using Cupscale.IO;
using Cupscale.UI;
using static Cupscale.UI.MainUIHelper;

namespace Cupscale.Main
{
    internal class Upscale
    {
        public enum ExportFormats
        {
            PNG,
            SameAsSource,
            JPEG,
            WEBP,
            BMP,
            TGA,
            DDS
        }

        public enum Filter
        {
            Mitchell,
            Bicubic,
            NearestNeighbor
        }

        public enum Overwrite
        {
            No,
            Yes
        }

        public enum ScaleMode
        {
            Percent,
            PixelsHeight,
            PixelsWidth,
            PixelsShorterSide,
            PixelsLongerSide
        }

        public enum UpscaleMode
        {
            Preview,
            Single,
            Batch
        }

        public static UpscaleMode currentMode = UpscaleMode.Preview;
        public static Overwrite overwriteMode = Overwrite.No;

        public static async Task CopyImagesTo(string path)
        {
            Program.lastOutputDir = path;
            Program.mainForm.AfterFirstUpscale();
            if (overwriteMode == Overwrite.Yes)
            {
                Logger.Log("Overwrite mode - removing suffix from filenames");
                IOUtils.ReplaceInFilenamesDir(Paths.imgOutPath, "-" + Program.lastModelName, "");
            }
            else
            {
                Logger.Log("Overwrite is off - keeping suffix.");
            }

            IOUtils.Copy(Paths.imgOutPath, path);
            await Task.Delay(1);
            IOUtils.DeleteContentsOfDir(Paths.imgInPath);
            IOUtils.DeleteContentsOfDir(Paths.imgOutPath);
        }

        public static async Task AddModelSuffix(string path)
        {
            var d = new DirectoryInfo(path);
            var files = d.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files) // Remove PNG extensions
            {
                var pathNoExt = Path.ChangeExtension(file.FullName, null);
                var ext = Path.GetExtension(file.FullName);
                File.Move(file.FullName,
                    pathNoExt + "-" + Program.lastModelName.Replace(":", ".").Replace(">>", "+") + ext);
                await Task.Delay(1);
            }
        }

        public static ModelData GetModelData()
        {
            var mdl = new ModelData();

            if (MainUIHelper.currentMode == Mode.Single)
            {
                var mdl1 = Program.currentModel1;
                if (string.IsNullOrWhiteSpace(mdl1)) return mdl;
                mdl = new ModelData(mdl1, null, ModelData.ModelMode.Single);
            }

            if (MainUIHelper.currentMode == Mode.Interp)
            {
                var mdl1 = Program.currentModel1;
                var mdl2 = Program.currentModel2;
                if (string.IsNullOrWhiteSpace(mdl1) || string.IsNullOrWhiteSpace(mdl2)) return mdl;
                mdl = new ModelData(mdl1, mdl2, ModelData.ModelMode.Interp, interpValue);
            }

            if (MainUIHelper.currentMode == Mode.Chain)
            {
                var mdl1 = Program.currentModel1;
                var mdl2 = Program.currentModel2;
                if (string.IsNullOrWhiteSpace(mdl1) || string.IsNullOrWhiteSpace(mdl2)) return mdl;
                mdl = new ModelData(mdl1, mdl2, ModelData.ModelMode.Chain);
            }

            if (MainUIHelper.currentMode == Mode.Advanced)
                mdl = new ModelData(null, null, ModelData.ModelMode.Advanced);

            return mdl;
        }

        public static async Task PostprocessingSingle(string path, bool dontResize = false)
        {
            Logger.Log("PostprocessingSingle: " + path);
            var newPath = "";
            if (Path.GetExtension(path) != ".tmp")
                newPath = path.Substring(0, path.Length - 8);
            else
                newPath = path.Substring(0, path.Length - 4);
            File.Move(path, newPath);
            path = newPath;
            Logger.Log("PostprocessingSingle New Path: " + path);

            if (outputFormat.Text == ExportFormats.PNG.ToStringTitleCase())
                await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Png50, dontResize);
            if (outputFormat.Text == ExportFormats.SameAsSource.ToStringTitleCase())
                await ImageProcessing.ConvertImageToOriginalFormat(path, true, false, dontResize);
            if (outputFormat.Text == ExportFormats.JPEG.ToStringTitleCase())
                await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Jpeg, dontResize);
            if (outputFormat.Text == ExportFormats.WEBP.ToStringTitleCase())
                await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.Weppy, dontResize);
            if (outputFormat.Text == ExportFormats.BMP.ToStringTitleCase())
                await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.BMP, dontResize);
            if (outputFormat.Text == ExportFormats.TGA.ToStringTitleCase())
                await ImageProcessing.PostProcessImage(path, ImageProcessing.Format.TGA, dontResize);
            if (outputFormat.Text == ExportFormats.DDS.ToStringTitleCase())
                await ImageProcessing.PostProcessDDS(path);
        }

        public static string FilenamePostprocess(string file)
        {
            try
            {
                var newFilename = file;

                var pathNoExt = Path.ChangeExtension(file, null);
                var ext = Path.GetExtension(file);

                newFilename = pathNoExt + "-" + Program.lastModelName.Replace(":", ".").Replace(">>", "+") + ext;

                File.Move(file, newFilename);
                newFilename = IOUtils.RenameExtension(newFilename, "jpg", Config.Get("jpegExtension"));

                return newFilename;
            }
            catch (Exception e)
            {
                Logger.ErrorMessage("Error during FilenamePostprocess(): ", e);
                return null;
            }
        }
    }
}