using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using CG.Web.MegaApiClient;
using Ookii.Dialogs.Wpf;

namespace Pikuniku_Save_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static class Globals
        {
            public static string docPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Pikuniku Save Editor");
            public static string resolution;
            public static bool fullscreen;
        }

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(Globals.docPath);
            Directory.CreateDirectory(Path.Combine(Globals.docPath, "Saves"));
            FetchSaves();
            ReadSettings();
        }

        private void FetchSaves()
        {
            var saves = Directory.GetFiles(Path.Combine(Globals.docPath, "Saves"));
            foreach (string saveFile in saves)
            {
                save.Items.Add(Path.GetFileNameWithoutExtension(saveFile));
            }
        }

        private void ReadSettings()
        {
            if (!File.Exists(Path.Combine(Globals.docPath, "settings.ini")))
            {
                CreateSettings();
                return;
            }
            var settings = File.ReadAllLines(Path.Combine(Globals.docPath, "settings.ini"));
            int i = 0;
            foreach (var line in settings)
            {
                if (line.StartsWith("//"))
                {
                    continue;
                }
                else if (line.StartsWith("Default resolution:"))
                {
                    try
                    {
                        res.Text = line.Split(':')[1].Trim();
                    }
                    catch
                    {
                        MessageBox.Show("Your settings file is corrupt. I will create a new one.", "Error");
                        CreateSettings();
                        return;
                    }
                    i++;
                }
                else if (line.StartsWith("Default fullscreen: "))
                {
                    try
                    {
                        fs.IsChecked = bool.Parse(line.Split(':')[1].Trim());
                    }
                    catch
                    {
                        MessageBox.Show("Your settings file is corrupt. I will create a new one.", "Error");
                        CreateSettings();
                        return;
                    }
                    i++;
                }
                else if (line.StartsWith("Default save: "))
                {
                    if (line.Count(x => x == ':') >= 2)
                    {
                        if (line.Split(':')[2].Contains(".reg"))
                        {
                            save.Items.Add(line.Split(':')[1].Trim() + ':' + line.Split(':')[2].Trim());
                            save.Text = line.Split(':')[1].Trim() + ':' + line.Split(':')[2].Trim();
                        }
                    }
                    else
                    {
                        save.Text = line.Split(':')[1].Trim();
                    }
                    i++;
                }
            }
            if (i != 3)
            {
                MessageBox.Show("Your settings file is corrupt. I will create a new one.", "Error");
                CreateSettings();
            }
        }

        private void CreateSettings()
        {
            File.WriteAllText(Path.Combine(Globals.docPath, "settings.ini"), "// Saved settings for the Pikuniku Save Editor. Don't edit this file. Or do. I don't really care. \nDefault resolution: 1920 x 1080 \nDefault fullscreen: true \nDefault save: ");
            res.SelectedIndex = 1;
            fs.IsChecked = true;
        }

        private void ChangeSettings(string res, string fullscreen, string save)
        {
            var settings = File.ReadAllLines(Path.Combine(Globals.docPath, "settings.ini"));
            int i = 0;
            foreach (string line in settings)
            {
                if (line.StartsWith("Default resolution: "))
                {
                    settings[i] = $"Default resolution: {res}";
                }
                else if (line.StartsWith("Default fullscreen: "))
                {
                    settings[i] = $"Default fullscreen: {fullscreen}";
                }
                else if (line.StartsWith("Default save: "))
                {
                    settings[i] = $"Default save: {save}";
                }
                i++;
            }
            File.WriteAllLines(Path.Combine(Globals.docPath, "settings.ini"), settings);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var browseDialog = new VistaOpenFileDialog();
            browseDialog.DefaultExt = ".reg";
            browseDialog.Title = "Select a Pikuniku save .reg file.";
            if (browseDialog.ShowDialog() == true)
            {
                save.Items.Add(browseDialog.FileName);
                save.Text = browseDialog.FileName;
            }
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Downloading saves. This may take a while.", "Notice");
            prog.Visibility = Visibility.Visible;
            progTotal.Visibility = Visibility.Visible;
            foreach (string file in Directory.GetFiles(Path.Combine(Globals.docPath, "Saves")))
            {
                File.Delete(file);
            }
            var client = new MegaApiClient();
            client.LoginAnonymous();
            Uri folderLink = new Uri("https://mega.nz/folder/TbowXKpJ#YoyVft8gTkmtzRvstBtF3w");
            IEnumerable<INode> nodes = client.GetNodesFromLink(folderLink);
            foreach (INode node in nodes.Where(x => x.Type == NodeType.File))
            {
                IProgress<double> progressHandler = new Progress<double>(x => prog.Value = x);
                Console.WriteLine($"Downloading {node.Name}");
                await client.DownloadFileAsync(node, Path.Combine(Globals.docPath, "Saves", node.Name), progressHandler);
                progTotal.Value = progTotal.Value + (100 / nodes.Where(x => x.Type == NodeType.File).Count());
            }
            MessageBox.Show($"Download has completed. Saves have been saved to {Path.Combine(Globals.docPath, "Saves")}.", "Notice");
            client.Logout();
            prog.Visibility = Visibility.Hidden;
            progTotal.Visibility = Visibility.Hidden;
            FetchSaves();
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            string directory = "null";
            if (File.Exists(save.Text))
            {
                directory = save.Text;
            }
            else
            {
                directory = (Path.Combine(Globals.docPath, "Saves", save.Text + ".reg"));
            }
            ChangeSettings(res.Text, fs.IsChecked.ToString(), save.Text);
            var regFile = File.ReadAllLines(Path.Combine(Globals.docPath, "Saves", directory));
            int i = 0;
            foreach (string line in regFile)
            {
                if (line.StartsWith("\"Screenmanager Resolution Width"))
                {
                    regFile[i] = line.Split(':')[0] + $":{Int32.Parse(res.Text.Split('x')[0].Trim()).ToString("x8")}";
                }
                else if (line.StartsWith("\"Screenmanager Resolution Height"))
                {
                    regFile[i] = line.Split(':')[0] + $":{Int32.Parse(res.Text.Split('x')[1].Trim()).ToString("x8")}";
                }
                else if (line.StartsWith("\"Screenmanager Is Fullscreen mode"))
                {
                    regFile[i] = $"\"Screenmanager Is Fullscreen mode_h3981298716\"=dword:0000000{((bool)fs.IsChecked ? 1 : 0)}";
                    break;
                }
                i++;
            }
            File.WriteAllLines(Path.Combine(Globals.docPath, "Saves", directory), regFile);
            string dir = Path.Combine(Globals.docPath, "Saves", directory);
            Process regeditProcess = Process.Start("regedit.exe", "/s \"" + directory + "\"");
            regeditProcess.WaitForExit();
            MessageBox.Show("Save has been successfully edited!");
        }
    }
}
