using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;
using Cupscale.ImageUtils;
using Cupscale.UI;
using ImageMagick;
using Paths = Cupscale.IO.Paths;

namespace Cupscale
{
    internal class PreviewMerger
    {
        public static float offsetX;
        public static float offsetY;
        public static string inputCutoutPath;
        public static string outputCutoutPath;

        public static bool showingOriginal;

        public static void Merge()
        {
            Program.mainForm.SetProgress(100f);
            inputCutoutPath = Path.Combine(Paths.previewPath, "preview.png.png");
            outputCutoutPath = Directory.GetFiles(Paths.previewOutPath, "*.png.*", SearchOption.AllDirectories)[0];

            var sourceImg = ImgUtils.GetImage(Paths.tempImgPath);
            var scale = GetScale();
            if (sourceImg.Width * scale > 16000 || sourceImg.Height * scale > 16000)
            {
                MergeOnlyCutout();
                MessageBox.Show("The scaled output image is very large (>16000px), so only the cutout will be shown.",
                    "Warning");
                return;
            }

            MergeScrollable();
        }

        private static void MergeScrollable()
        {
            if (offsetX < 0f) offsetX *= -1f;
            if (offsetY < 0f) offsetY *= -1f;
            var scale = GetScale();
            offsetX *= scale;
            offsetY *= scale;
            Logger.Log("Merging " + outputCutoutPath + " onto " + Program.lastFilename + " using offset " + offsetX +
                       "x" + offsetY);
            var image = MergeInMemory(scale);
            MainUIHelper.currentOriginal = ImgUtils.GetImage(Paths.tempImgPath);
            MainUIHelper.currentOutput = image;
            MainUIHelper.currentScale =
                ImgUtils.GetScaleFloat(ImgUtils.GetImage(inputCutoutPath), ImgUtils.GetImage(outputCutoutPath));
            UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, image);
            Program.mainForm.SetProgress(0f, "Done.");
        }


        public static Image MergeInMemory(int scale)
        {
            var tempScaledSourceImagePath = Path.Combine(Paths.tempImgPath.GetParentDir(), "scaled-source.png");
            var scaledSourceMagickImg = new MagickImage(Paths.tempImgPath);
            var oldWidth = scaledSourceMagickImg.Width;
            Logger.Log("oldWidth: " + oldWidth);
            scaledSourceMagickImg = ImageProcessing.ResizeImagePre(scaledSourceMagickImg);
            var newWidth = scaledSourceMagickImg.Width;
            Logger.Log("newWidth: " + newWidth);
            scaledSourceMagickImg.Write(tempScaledSourceImagePath);
            var scaledSourceImg = ImgUtils.GetImage(tempScaledSourceImagePath);

            var preScale = oldWidth / (float) newWidth;
            Logger.Log("Pre Scale: " + preScale + "x");

            var cutout = ImgUtils.GetImage(outputCutoutPath);

            if (scaledSourceImg.Width * scale == cutout.Width && scaledSourceImg.Height * scale == cutout.Height)
            {
                Logger.Log("Cutout is the entire image - skipping merge");
                return cutout;
            }

            var destImage = new Bitmap(scaledSourceImg.Width * scale, scaledSourceImg.Height * scale);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                if (Program.currentFilter == FilterType.Point)
                    graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(scaledSourceImg, 0, 0, destImage.Width, destImage.Height); // Scale up
                graphics.DrawImage(cutout, (offsetX / preScale).RoundToInt(),
                    (offsetY / preScale).RoundToInt()); // Overlay cutout
            }

            return destImage;
        }

        private static void MergeOnlyCutout()
        {
            var scale = GetScale();

            var originalCutout = ImgUtils.GetMagickImage(inputCutoutPath);
            originalCutout.FilterType = Program.currentFilter;
            originalCutout.Resize(new Percentage(scale * 100));
            var scaledCutoutPath = Path.Combine(Paths.previewOutPath, "preview-input-scaled.png");
            originalCutout.Format = MagickFormat.Png;
            originalCutout.Quality = 0; // Save preview as uncompressed PNG for max speed
            originalCutout.Write(scaledCutoutPath);

            MainUIHelper.currentOriginal = ImgUtils.GetImage(scaledCutoutPath);
            MainUIHelper.currentOutput = ImgUtils.GetImage(outputCutoutPath);

            MainUIHelper.previewImg.Image = MainUIHelper.currentOutput;
            MainUIHelper.previewImg.ZoomToFit();
            MainUIHelper.previewImg.Zoom = (int) Math.Round(MainUIHelper.previewImg.Zoom * 1.01f);
            Program.mainForm.resetImageOnMove = true;
            Program.mainForm.SetProgress(0f, "Done.");
        }

        private static int GetScale()
        {
            var val = ImgUtils.GetMagickImage(inputCutoutPath);
            var val2 = ImgUtils.GetMagickImage(outputCutoutPath);
            var result = (int) Math.Round(val2.Width / (float) val.Width);
            Logger.Log("Preview Merger Scale: " + inputCutoutPath + " -> " + outputCutoutPath + " = " + result + "x");
            return result;
        }

        public static void ShowOutput()
        {
            if (MainUIHelper.currentOutput != null)
            {
                showingOriginal = false;
                UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, MainUIHelper.currentOutput);
            }
        }

        public static void ShowOriginal()
        {
            if (MainUIHelper.currentOriginal != null)
            {
                showingOriginal = true;
                UIHelpers.ReplaceImageAtSameScale(MainUIHelper.previewImg, MainUIHelper.currentOriginal);
            }
        }
    }
}