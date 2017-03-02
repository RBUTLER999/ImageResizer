using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;
using Size = System.Drawing.Size;

namespace ImageResizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public static IEnumerable<string> GetAllFiles(string path, Func<FileInfo, bool> checkFile = null)
        {
            string mask = Path.GetFileName(path);
            if (string.IsNullOrEmpty(mask))
                mask = "*.*";
            path = Path.GetDirectoryName(path);
            string[] files = Directory.GetFiles(path, mask, SearchOption.AllDirectories);
            foreach (string file in files)
            {
                if (checkFile == null || checkFile(new FileInfo(file)))
                    yield return file;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string sourceDir = txtSource.Text.TrimEnd('\\') + "\\"; ;
            string destDir = txtDesitination.Text.TrimEnd('\\') + "\\";
            
            IEnumerable<string> files = GetAllFiles(sourceDir, CheckFile).ToList();

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 2 }, s => 
            {
                string fullPath = Path.GetFullPath(s);

                string destinationFullPath = destDir + fullPath.Replace(sourceDir, string.Empty);

                if (HasJpegExtension(fullPath))
                {
                    Resize(fullPath, destinationFullPath);

                }
            });

            MessageBox.Show("finished");
        }

        private bool CheckFile(FileInfo fileInfo)
        {
            return HasJpegExtension(fileInfo.Name);
        }

        static bool HasJpegExtension(string filename)
        {
            // add other possible extensions here
            return Path.GetExtension(filename).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                || Path.GetExtension(filename).Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase);
        }

        private bool Resize(string sourceFileName, string destinationFileName)
        {
            // The wolf made a change
            // So I'm going to try and push this back
          
            if (!File.Exists(destinationFileName))
            {
                float width = 0;
                float height = 0;

                using (FileStream file = new FileStream(sourceFileName, FileMode.Open, FileAccess.Read))
                {
                    using (Image tif = Image.FromStream(file, false, false))
                    {
                        width = tif.Width / 2;
                        height = tif.Height / 2;
                        
                        while (Math.Min(width,height) > 1500)
                        {
                            width = width / 2;
                            height = height/2;
                        }
                    }
                }

                // Read a file and resize it.
                byte[] photoBytes = File.ReadAllBytes(sourceFileName);
                int quality = 70;

                JpegFormat format = new JpegFormat();
            
                Size size = new Size((int) width, (int) height);
                
                using (MemoryStream inStream = new MemoryStream(photoBytes))
                {
                        using (ImageFactory imageFactory = new ImageFactory(true))
                        {
                            ResizeLayer resizeLayer = new ResizeLayer(size, ImageProcessor.Imaging.ResizeMode.Crop);
                            // Load, resize, set the format and quality and save an image.
                            imageFactory.Load(inStream)
                                        .Resize(resizeLayer)
                                        .Format(format)
                                        .Quality(quality)
                                        .Save(destinationFileName);
                        }

                }
            }
            return true;
        }
    }
}
