using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media.Imaging;
using DdsFileTypePlus;
using ImageMagick;
using PaintDotNet;

namespace Cupscale.ImageUtils
{
    internal class ImgUtils
    {
        public static Image GetImage(string path)
        {
            Logger.Log("IOUtils.GetImage: Reading Image from " + path);
            using var stream = new MemoryStream(File.ReadAllBytes(path));
            var img = Image.FromStream(stream);
            Logger.Log("[OK]", true, true);
            return img;
        }

        public static MagickImage GetMagickImage(string path)
        {
            Logger.Log("IOUtils.GetMagickImage: Reading Image from " + path);
            MagickImage image;
            if (Path.GetExtension(path).ToLower() == ".dds")
                try
                {
                    image = new MagickImage(
                        path); // Try reading DDS with IM, fall back to DdsFileTypePlusHack if it fails
                }
                catch
                {
                    Logger.Log("Failed to read DDS using Mackig.NET - Trying DdsFileTypePlusHack");
                    try
                    {
                        var surface = DdsFile.Load(path);
                        image = ConvertToMagickImage(surface);
                        image.HasAlpha = DdsFile.HasTransparency(surface);
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorMessage("This DDS format appears to be incompatible.", e);
                        return null;
                    }
                }
            else
                image = new MagickImage(path);

            Logger.Log("[OK]", true, true);
            return image;
        }

        public static MagickImage ConvertToMagickImage(Surface surface)
        {
            MagickImage result;
            var bitmap = surface.CreateAliasedBitmap();
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Png);
                memoryStream.Position = 0;
                result = new MagickImage(memoryStream, new MagickReadSettings {Format = MagickFormat.Png00});
            }

            return result;
        }

        public static float GetScale(Image imgFrom, Image imgTo)
        {
            return (int) Math.Round(GetScaleFloat(imgFrom, imgTo));
        }

        public static float GetScaleFloat(Image imgFrom, Image imgTo)
        {
            return imgTo.Width / (float) imgFrom.Width;
        }

        public static MagickImage FillAlphaWithBgColor(MagickImage img)
        {
            return FillAlphaWithColor(img, new MagickColor("#" + Config.Get("alphaBgColor")));
        }

        public static MagickImage FillAlphaWithColor(MagickImage img, MagickColor color)
        {
            var bg = new MagickImage(color, img.Width, img.Height);
            bg.BackgroundColor = color;
            bg.Composite(img, CompositeOperator.Over);

            return bg;
        }

        public static int GetColorDepth(string path)
        {
            var source = new BitmapImage(new Uri(path));
            return source.Format.BitsPerPixel;
        }
    }
}