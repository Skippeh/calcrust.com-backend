using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer
{
    static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length <= 0)
            {
                Console.WriteLine("No directory specified.");
                return;
            }

            string directory = args[0];
            string imagesDirectory = null;
            int width;
            int height;

            if (args.Length <= 2)
            {
                Console.WriteLine("Image width and height not specified.");
                return;
            }
            else
            {
                width = int.Parse(args[1]);
                height = int.Parse(args[2]);

                if (width <= 0)
                    Console.Error.WriteLine("Width <= 0");

                if (height <= 0)
                    Console.Error.WriteLine("Height <= 0");
            }

            if (args.Length <= 3)
            {
                Console.WriteLine("Rust images directory not specified, not copying images.");
            }
            else
            {
                imagesDirectory = args[3];
            }

            if (imagesDirectory != null)
            {
                foreach (var path in Directory.GetFiles(imagesDirectory, "*.png", SearchOption.TopDirectoryOnly))
                {
                    try
                    {
                        string fileName = Path.GetFileName(path);
                        File.Copy(path, Path.Combine(directory, fileName), true);
                        Console.WriteLine("Copied " + fileName + ".");
                    }
                    catch (IOException ex)
                    {
                        Console.WriteLine("Failed to copy image: " + ex.Message);
                    }
                }
            }
            
            var images = Directory.GetFiles(directory, "*.png");

            foreach (var path in images)
            {
                if (path.EndsWith("_small.png", true, CultureInfo.InvariantCulture))
                    continue;

                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var bitmap = new Bitmap(stream))
                    {
                        using (var newBitmap = GetThumbnailImage(bitmap, new Size(width, height)))
                        {
                            var filename = Path.GetFileNameWithoutExtension(path) + "_small.png";
                            newBitmap.Save(Path.Combine(directory, filename), ImageFormat.Png);
                        }
                    }
                }
            }
        }

        public static Image GetThumbnailImage(Image OriginalImage, Size ThumbSize)
        {
            Int32 thWidth = ThumbSize.Width;
            Int32 thHeight = ThumbSize.Height;
            Image i = OriginalImage;
            Int32 w = i.Width;
            Int32 h = i.Height;
            Int32 th = thWidth;
            Int32 tw = thWidth;
            if (h > w)
            {
                Double ratio = (Double)w / (Double)h;
                th = thHeight < h ? thHeight : h;
                tw = thWidth < w ? (Int32)(ratio * thWidth) : w;
            }
            else
            {
                Double ratio = (Double)h / (Double)w;
                th = thHeight < h ? (Int32)(ratio * thHeight) : h;
                tw = thWidth < w ? thWidth : w;
            }
            Bitmap target = new Bitmap(tw, th);
            Graphics g = Graphics.FromImage(target);
            g.SmoothingMode = SmoothingMode.HighQuality;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.High;
            Rectangle rect = new Rectangle(0, 0, tw, th);
            g.DrawImage(i, rect, 0, 0, w, h, GraphicsUnit.Pixel);
            return (Image)target;
        }
    }
}