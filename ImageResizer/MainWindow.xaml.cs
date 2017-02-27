using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ImageProcessor.Imaging;

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
                string fullPath = System.IO.Path.GetFullPath(s);

                string destinationFullPath = destDir + fullPath.Replace(sourceDir, string.Empty);

                if (HasJpegExtension(fullPath))
                {
                    Resize(fullPath, destinationFullPath);

                }
            });

            System.Windows.MessageBox.Show("finished");
        }

        private bool CheckFile(FileInfo fileInfo)
        {
            return HasJpegExtension(fileInfo.Name);
        }

        static bool HasJpegExtension(string filename)
        {
            // add other possible extensions here
            return System.IO.Path.GetExtension(filename).Equals(".jpg", StringComparison.InvariantCultureIgnoreCase)
                || System.IO.Path.GetExtension(filename).Equals(".jpeg", StringComparison.InvariantCultureIgnoreCase);
        }

        private bool Resize(string sourceFileName, string destinationFileName)
        {
            if (!System.IO.File.Exists(destinationFileName))
            {
                float width = 0;
                float height = 0;

                using (System.IO.FileStream file = new System.IO.FileStream(sourceFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                {
                    using (System.Drawing.Image tif = System.Drawing.Image.FromStream(stream: file,
                                            useEmbeddedColorManagement: false,
                                            validateImageData: false))
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
                byte[] photoBytes = System.IO.File.ReadAllBytes(sourceFileName);
                int quality = 70;

                ImageProcessor.Imaging.Formats.JpegFormat format = new ImageProcessor.Imaging.Formats.JpegFormat();
            
                System.Drawing.Size size = new System.Drawing.Size((int) width, (int) height);
                
                using (System.IO.MemoryStream inStream = new System.IO.MemoryStream(photoBytes))
                {
                        using (ImageProcessor.ImageFactory imageFactory = new ImageProcessor.ImageFactory(true))
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
