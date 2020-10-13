using System;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cupscale.FFmpeg;
using Cupscale.Forms;
using Cupscale.ImageUtils;
using Cupscale.IO;
using Cupscale.Main;
using Cupscale.UI;

namespace Cupscale.Preview
{
    internal class ClipboardPreview
    {
        public static Bitmap originalPreview;
        public static Bitmap resultPreview;

        public static async void CopyToClipboardSideBySide(bool saveToFile, bool fullImage = false)
        {
            var footerHeight = 45;

            try
            {
                if (fullImage)
                {
                    originalPreview =
                        new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview-input-scaled.png")));
                    resultPreview =
                        new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview-merged.png")));
                }
                else
                {
                    originalPreview =
                        new Bitmap(ImgUtils.GetImage(Directory.GetFiles(Paths.previewPath, "*.png.*",
                            SearchOption.AllDirectories)[0]));
                    resultPreview = new Bitmap(ImgUtils.GetImage(Directory.GetFiles(Paths.previewOutPath, "*.png.*",
                        SearchOption.AllDirectories)[0]));
                }
            }
            catch
            {
                MessageBox.Show("Error creating clipboard preview!", "Error");
            }

            var comparisonMod = 1;
            int newWidth = comparisonMod * resultPreview.Width, newHeight = comparisonMod * resultPreview.Height;

            var outputImage = new Bitmap(2 * newWidth, newHeight + footerHeight);
            var modelName = Program.lastModelName;
            using (var graphics = Graphics.FromImage(outputImage))
            {
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.DrawImage(originalPreview, new Rectangle(0, 0, newWidth, newHeight),
                    new Rectangle(new Point(), originalPreview.Size), GraphicsUnit.Pixel);
                graphics.DrawImage(resultPreview, new Rectangle(newWidth, 0, newWidth, newHeight),
                    new Rectangle(new Point(), resultPreview.Size), GraphicsUnit.Pixel);

                var Bmp = new Bitmap(2 * newWidth, footerHeight);
                var color = Color.FromArgb(22, 22, 22);
                using (var gfx = Graphics.FromImage(Bmp))
                using (var brush = new SolidBrush(color))
                {
                    gfx.FillRectangle(brush, 0, 0, 2 * newWidth, footerHeight);
                }

                graphics.DrawImage(Bmp, 0, newHeight);

                var p = new GraphicsPath();
                var fontSize = 19;
                SizeF s = new Size(999999999, 99999999);

                var font = new Font("Times New Roman", graphics.DpiY * fontSize / 72);

                var barString = "[CS] " + Path.GetFileName(Program.lastFilename) + " - " + modelName;

                int cf = 0, lf = 0;
                while (s.Width >= 2 * newWidth)
                {
                    fontSize--;
                    font = new Font(FontFamily.GenericSansSerif, graphics.DpiY * fontSize / 72, FontStyle.Regular);
                    s = graphics.MeasureString(barString, font, new SizeF(), new StringFormat(), out cf, out lf);
                }

                var stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;

                double a = graphics.DpiY * fontSize / 72;
                stringFormat.LineAlignment = StringAlignment.Center;

                var contrastW = GetColorContrast(color, Color.White);
                var contrastB = GetColorContrast(color, Color.Black);
                var textBrush = contrastW < 3.0 ? Brushes.Black : Brushes.White;

                graphics.DrawString(
                    $"{barString}",
                    font,
                    textBrush,
                    new Rectangle(0, newHeight, 2 * newWidth, footerHeight - 0),
                    stringFormat);
            }

            try
            {
                if (saveToFile)
                    await SaveComparisonToFile(outputImage);
                else
                    Clipboard.SetDataObject(outputImage);
            }
            catch
            {
                MessageBox.Show("Failed to save comparison.", "Error");
            }
        }

        public static async void CopyToClipboardSlider(bool saveToFile, bool fullImage = false)
        {
            var footerHeight = 45;

            try
            {
                if (fullImage)
                {
                    originalPreview =
                        new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview-input-scaled.png")));
                    resultPreview =
                        new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview-merged.png")));
                }
                else
                {
                    originalPreview = new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewPath, "preview.png.png")));
                    resultPreview =
                        new Bitmap(ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview.png.tmp")));
                }
            }
            catch
            {
                MessageBox.Show("Error creating clipboard preview!", "Error");
            }


            var comparisonMod = 1;
            //int.TryParse(comparisonMod_comboBox.SelectedValue.ToString(), out comparisonMod);
            int newWidth = comparisonMod * resultPreview.Width, newHeight = comparisonMod * resultPreview.Height;

            var outputImage = new Bitmap(newWidth, newHeight + footerHeight);
            var modelName = Program.lastModelName;
            using (var graphics = Graphics.FromImage(outputImage))
            {
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                graphics.CompositingQuality = CompositingQuality.HighQuality;

                var halfWidth = (int) Math.Round(newWidth * 0.5f);
                var croppedOutput = resultPreview.Clone(new Rectangle(halfWidth, 0, newWidth - halfWidth, newHeight),
                    resultPreview.PixelFormat);

                graphics.DrawImage(originalPreview, 0, 0, newWidth, newHeight); // First half
                graphics.DrawImage(croppedOutput, halfWidth, 0); // Second half
                graphics.FillRectangle(new SolidBrush(Color.FromArgb(192, Color.Black)), halfWidth - 2, 0, 4,
                    newHeight); // Line

                var Bmp = new Bitmap(newWidth, footerHeight);
                var color = Color.FromArgb(22, 22, 22);
                using (var gfx = Graphics.FromImage(Bmp))
                using (var brush = new SolidBrush(color))
                {
                    gfx.FillRectangle(brush, 0, 0, newWidth, footerHeight);
                    ;
                }

                graphics.DrawImage(Bmp, 0, newHeight);

                var p = new GraphicsPath();
                var fontSize = 19;
                SizeF s = new Size(999999999, 99999999);

                var font = new Font("Times New Roman", graphics.DpiY * fontSize / 72);

                var barString = "[CS] " + Path.GetFileName(Program.lastFilename) + " - " + modelName;

                int cf = 0, lf = 0;
                while (s.Width >= newWidth)
                {
                    fontSize--;
                    font = new Font(FontFamily.GenericSansSerif, graphics.DpiY * fontSize / 72, FontStyle.Regular);
                    s = graphics.MeasureString(barString, font, new SizeF(), new StringFormat(), out cf, out lf);
                }

                var stringFormat = new StringFormat();
                stringFormat.Alignment = StringAlignment.Center;

                double a = graphics.DpiY * fontSize / 72;
                stringFormat.LineAlignment = StringAlignment.Center;

                var contrastW = GetColorContrast(color, Color.White);
                var contrastB = GetColorContrast(color, Color.Black);

                var textBrush = contrastW < 3.0 ? Brushes.Black : Brushes.White;

                graphics.DrawString(
                    $"{barString}",
                    font,
                    textBrush,
                    new Rectangle(0, newHeight, newWidth, footerHeight - 0),
                    stringFormat);
            }

            try
            {
                if (saveToFile)
                    await SaveComparisonToFile(outputImage);
                else
                    Clipboard.SetDataObject(outputImage);
            }
            catch
            {
                MessageBox.Show("Failed to save comparison.", "Error");
            }
        }

        private static async Task SaveComparisonToFile(Image outputImage)
        {
            var comparisonSavePath = Path.ChangeExtension(Program.lastFilename, null) + "-comparison.png";
            outputImage.Save(comparisonSavePath);
            await ImageProcessing.ConvertImage(comparisonSavePath, GetSaveFormat(), false,
                ImageProcessing.ExtensionMode.UseNew);
            MessageBox.Show("Saved current comparison to:\n\n" + Path.ChangeExtension(comparisonSavePath, null),
                "Message");
        }

        private static Bitmap CropImage(Bitmap source, Rectangle section)
        {
            var bitmap = new Bitmap(section.Width, section.Height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);
                return bitmap;
            }
        }

        private static double GetColorContrast(Color background, Color text)
        {
            var L1 = 0.2126 * background.R / 255 + 0.7152 * background.G / 255 + 0.0722 * background.B / 255;
            var L2 = 0.2126 * text.R / 255 + 0.7152 * text.G / 255 + 0.0722 * text.B / 255;
            if (L1 > L2)
                return (L1 + 0.05) / (L2 + 0.05);
            return (L2 + 0.05) / (L1 + 0.05);
        }

        public static async void BeforeAfterAnim(bool save, bool h264)
        {
            var ext = "gif";
            if (h264) ext = "mp4";

            var dialogForm = new DialogForm("Creating comparison " + ext.ToUpper() + "...");

            var tempPath = Path.Combine(IOUtils.GetAppDataDir(), "giftemp");
            var framesPath = Path.Combine(tempPath, "frames");
            IOUtils.DeleteContentsOfDir(tempPath);
            Directory.CreateDirectory(framesPath);

            var img1 = Path.Combine(Paths.previewPath, "preview.png.png");
            var img2 = Path.Combine(Paths.previewOutPath, "preview.png.tmp");

            var image1 = ImgUtils.GetImage(img1);
            var image2 = ImgUtils.GetImage(img2);
            var scale = image2.Width / (float) image1.Width;
            Logger.Log("Scale for animation: " + scale);

            var outpath = Path.Combine(tempPath, "comparison." + ext);

            if (image2.Width <= 2048 && image2.Height <= 2048)
            {
                ImgUtils.GetImage(img1).Scale(scale, InterpolationMode.NearestNeighbor)
                    .Save(Path.Combine(framesPath, "0.png"));
                File.Copy(img2, Path.Combine(framesPath, "1.png"), true);
                if (h264)
                {
                    await FFmpegCommands.FramesToOneFpsMp4(framesPath, false, 14, 9, "", false);
                    File.Move(Path.Combine(tempPath, "frames." + ext), outpath);
                }
                else
                {
                    await FFmpeg.FFmpeg.RunGifski(" -r 1 -W 2048 -q -o " + outpath.WrapPath() + " \"" + framesPath +
                                                  "/\"*.\"png\"");
                }

                if (save)
                {
                    var comparisonSavePath = Path.ChangeExtension(Program.lastFilename, null) + "-comparison." + ext;
                    File.Copy(outpath, comparisonSavePath, true);
                    dialogForm.Close();
                    MessageBox.Show("Saved current comparison to:\n\n" + comparisonSavePath, "Message");
                }
                else
                {
                    var paths = new StringCollection();
                    paths.Add(outpath);
                    Clipboard.SetFileDropList(paths);
                    dialogForm.Close();
                    MessageBox.Show("The " + ext.ToUpper() +
                                    " file has been copied. You can paste it into any folder.\n" +
                                    "Please note that pasting it into Discord or other programs won't work as the clipboard can't hold animated images.",
                        "Message");
                }
            }
            else
            {
                MessageBox.Show(
                    "The preview is too large for making an animation. Please create a smaller cutout or choose a different comparison type.",
                    "Error");
            }

            dialogForm.Close();
        }

        public static async void OnlyResult(bool saveToFile)
        {
            var outputImage = ImgUtils.GetImage(Path.Combine(Paths.previewOutPath, "preview.png.tmp"));
            try
            {
                if (saveToFile)
                    await SaveComparisonToFile(outputImage);
                else
                    Clipboard.SetDataObject(outputImage);
            }
            catch
            {
                MessageBox.Show("Failed to save comparison.", "Error");
            }
        }

        private static ImageProcessing.Format GetSaveFormat()
        {
            var saveFormat = ImageProcessing.Format.PngFast;
            if (Config.GetInt("previewFormat") == 1)
                saveFormat = ImageProcessing.Format.Jpeg;
            if (Config.GetInt("previewFormat") == 2)
                saveFormat = ImageProcessing.Format.Weppy;
            return saveFormat;
        }
    }
}