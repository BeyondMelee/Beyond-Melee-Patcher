using System;
using System.ComponentModel;
using System.Threading;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using System.Net;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Navigation;
using System.Windows;
using System.Windows.Media;
using System.Threading.Tasks;

namespace beyond_melee_installer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string filePath = "";

        private readonly string versionNumber = "1-1-1";

        private static readonly Uri beyondUri = new Uri("https://download1586.mediafire.com/8b4zgkzdu8mg/orwanf9crpzha3l/beyond_patch.xdelta", UriKind.Absolute);
        private static readonly Uri dietUri = new Uri("https://download1072.mediafire.com/nrmdbyxn6meg/ngcf4nwuo7bao9u/diet_beyond_patch.xdelta", UriKind.Absolute);

        private readonly BackgroundWorker worker = new BackgroundWorker();

        private static readonly WebClient webClient = new WebClient();

        //private Uri deltaUri = new Uri("pack://application:,,,/Resources/Patch.xdelta");

        public MainWindow()
        {
            InitializeComponent();
        }

        private void FilePanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 1)
                {
                    FileNameLabel.Text = string.Empty;
                    FileNameLabel2.Foreground = Brushes.Red;
                    FileNameLabel2.Text = "Please only drop one file in the window at a time.";
                    LinkText.Text = string.Empty;
                }

                else if (!Path.GetFileName(files[0]).Contains(".iso"))
                {
                    FileNameLabel.Text = string.Empty;
                    FileNameLabel2.Foreground = Brushes.Red;
                    FileNameLabel2.Text = "This does not seem to be an iso file. Please use an iso file.";
                    LinkText.Text = string.Empty;
                }
                else
                {
                    FileNameLabel.Text = string.Empty;
                    FileNameLabel2.Foreground = Brushes.Yellow;
                    FileNameLabel2.Text = "Checking iso file...";
                    worker.DoWork += delegate (object s, DoWorkEventArgs args)
                    {
                        string path = (string)args.Argument;
                        args.Result = GetMD5(path);
                    };

                    worker.RunWorkerCompleted += delegate (object s, RunWorkerCompletedEventArgs args)
                    {
                        string result = (string)args.Result;
                        CompareMD5(result, files[0]);
                    };

                    worker.RunWorkerAsync(files[0]);
                }
            }
        }

        private void patch_Click(object sender, RoutedEventArgs e)
        {
            if (filePath == "")
            {
                FileNameLabel2.Foreground = Brushes.Red;
                FileNameLabel2.Text = "Please drop a file in first.";
            }
            else
            {
                if (BeyondRadio.IsChecked == true)
                {
                    //for some reason this doesnt work
                    FileNameLabel2.Foreground = Brushes.Yellow;
                    FileNameLabel2.Text = "Running Patch...";
                    if (RunPatch("beyond", filePath, $"Beyond-Melee-{versionNumber}"))
                    {
                        FileNameLabel.Foreground = Brushes.LightGreen;
                        FileNameLabel.Text = "Success! Your file should be in the same folder as this patcher named";
                        FileNameLabel2.Foreground = Brushes.LightGreen;
                        FileNameLabel2.Text = $"'Beyond-Melee-{versionNumber}.iso'.";
                    }
                    else
                    {
                        FileNameLabel2.Foreground = Brushes.Red;
                        FileNameLabel2.Text = "Something went wrong. Check the #patcher-support channel in the Discord.";
                    }

                }
                else if (DietRadio.IsChecked == true)
                {
                    //for some reason this doesnt work
                    FileNameLabel2.Foreground = Brushes.Yellow;
                    FileNameLabel2.Text = "Running Patch...";

                    if ((bool)RunPatch("diet", filePath, $"Diet-Beyond-Melee-{versionNumber}"))
                    {
                        FileNameLabel.Foreground = Brushes.LightGreen;
                        FileNameLabel.Text = "Success! Your file should be in the same folder as this patcher named";
                        FileNameLabel2.Foreground = Brushes.LightGreen;
                        FileNameLabel2.Text = $"'Diet-Beyond-Melee-{versionNumber}.iso'.";
                    }
                    else
                    {
                        FileNameLabel2.Foreground = Brushes.Red;
                        FileNameLabel2.Text = "Something went wrong. Check the #patcher-support channel in the Discord.";
                    }

                }
                else
                {
                    FileNameLabel2.Foreground = Brushes.Red;
                    FileNameLabel2.Text = "Please choose a version";
                }
            }
        }

        public static async Task<bool> RunPatch(string patch, string isoPath, string versionName)
        {

            var xdeltaUri = new Uri("pack://application:,,,/Resources/xdelta.exe");
            var deltaUri = new Uri($"pack://application:,,,/Resources/{patch}.xdelta");

            string tmpFolder = Path.Join(Path.GetTempPath(), "BeyondMelee");
            Directory.CreateDirectory(tmpFolder);
            Trace.WriteLine($"Created tmpFolder at {tmpFolder}");

            string xdeltaPath = Path.Join(tmpFolder, "xdelta.exe");
            string deltaPath = Path.Join(tmpFolder, "Patch.xdelta");

            if (patch == "beyond")
            {
                await DownloadPatch(beyondUri, xdeltaPath);
            }
            else
            {
                await DownloadPatch(deltaUri, xdeltaPath);
            }
            

            //WriteResourceFile(xdeltaUri, xdeltaPath);
            //Trace.WriteLine($"Copied xdelta to {xdeltaPath}");
            //WriteResourceFile(deltaUri, deltaPath);
            //Trace.WriteLine($"Copied Patch to {deltaPath}");

            //Change the following string from /C to /K to leave the cmd window open for debugging
            string cmdTxt = $"-d -f -s \"{isoPath}\" \"{deltaPath}\" \"{versionName}\".iso";
            Process cmdPatch = new Process();
            cmdPatch.StartInfo.FileName = xdeltaPath;
            cmdPatch.StartInfo.Arguments = cmdTxt;
            cmdPatch.Start();
            cmdPatch.WaitForExit();


            //This can probably be shortened, I need to test it in order to see
            if (cmdPatch.ExitCode == 0)
            {
                Trace.WriteLine("xdelta exited with code 0");
                File.Delete(xdeltaPath);
                Trace.WriteLine($"Deleted xdelta");
                File.Delete(deltaPath);
                Trace.WriteLine($"Deleted patch");
                return true;
            }
            else
            {
                Trace.WriteLine("xdelta exited with different exit code than zero, error");
                File.Delete(xdeltaPath);
                Trace.WriteLine($"Deleted xdelta");
                File.Delete(deltaPath);
                Trace.WriteLine($"Deleted patch");
                return false;
            }
        }

        private static async Task<int> DownloadPatch(Uri uri, string path)
        {
            await webClient.DownloadFileTaskAsync(uri, path);
            Trace.WriteLine("Patch downloaded");
            return 1;
        }

        public static void WriteResourceFile(Uri uri, string path, int bufferLength = 4096)
        {
            var xdeltaResource = Application.GetResourceStream(uri);
            Stream xdeltaStream = File.OpenWrite(path);
            byte[] buffer = new byte[bufferLength];
            while (true)
            {
                var readCount = xdeltaResource.Stream.Read(buffer, 0, bufferLength);
                if (readCount == 0)
                {
                    xdeltaStream.Close();
                    break;
                }
                xdeltaStream.Write(buffer, 0, readCount);
            }
        }

        private void hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {

            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void BeyondRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!(filePath == ""))
            {
                PreviewImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/beyond_preview.png"));
                BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/beyond_banner.png"));
                VersionInfo.Text = "The standard version of Beyond Melee. All the great new features right in Melee.";
            }
            else
            {
                FileNameLabel2.Foreground = Brushes.Red;
                FileNameLabel2.Text = "Please drop a file in first.";
            }
        }

        private void DietRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (!(filePath == ""))
            {
                PreviewImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/diet_preview.png"));
                BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/diet_banner.png"));
                VersionInfo.Text = "Diet Beyond Melee is a lower quality version of Beyond Melee made to run on lower end hardware, like Diet Melee.";
            }
            else
            {
                FileNameLabel2.Foreground = Brushes.Red;
                FileNameLabel2.Text = "Please drop a file in first.";
            }
        }

        private string GetMD5(string filename)
        {
            using var stream = File.OpenRead(filename);
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(stream);
            return BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
        }

        private string CheckMD5(string hash)
        {
            switch (hash)
            {
                case "0e63d4223b01d9aba596259dc155a174":
                    return "valid";
                case "570f5ba46604d17f2d9c4fabe4b8c34d":
                    return "nkit";
                default:
                    return "invalid";
            }

        }

        private void CompareMD5(string hash, string filename)
        {
            if (CheckMD5(hash) == "invalid")
            {
                FileNameLabel.Foreground = Brushes.Red;
                FileNameLabel.Text = "This iso file seems to be modified.  Please use a completely vanilla NTSC 1.02 iso.";
                FileNameLabel2.Foreground = Brushes.Red;
                FileNameLabel2.Text = "(This includes any skins or visual mods, which also interfere with the patch.)";
                LinkText.Text = String.Empty;
            }
            else if (CheckMD5(hash) == "nkit")
            {
                FileNameLabel.Foreground = Brushes.Red;
                FileNameLabel.Text = "This application cannot process nkit compressed iso files.";
                FileNameLabel2.Foreground = Brushes.Red;
                FileNameLabel2.Text = "Please click the link below for a guide on how to decompress it.";
                LinkText.Text = "Guide for NKit decompression";
            }
            else
            {
                if (BeyondRadio.IsChecked == true)
                {
                    PreviewImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/beyond_preview.png"));
                    BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/beyond_banner.png"));
                    VersionInfo.Text = "The standard version of Beyond Melee. All the great new features right in Melee.";
                }
                else if (DietRadio.IsChecked == true)
                {
                    PreviewImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/diet_preview.png"));
                    BannerImage.Source = new BitmapImage(new Uri("pack://application:,,,/Images/diet_banner.png"));
                    VersionInfo.Text = "Diet Beyond Melee is a lower quality version of Beyond Melee made to run on lower end hardware, like Diet Melee.";
                }
                //Removes old text and changes colors to green
                FileNameLabel.Foreground = Brushes.LightGreen;
                FileNameLabel.Text = "";
                FileNameLabel2.Foreground = Brushes.LightGreen;
                FileNameLabel2.Text = "";
                LinkText.Text = "";

                filePath = Path.GetFullPath(filename);

                FileNameLabel.Text = Path.GetFileName(filename);
            }
        }
    }
}
