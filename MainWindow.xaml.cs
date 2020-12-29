using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows;

using Newtonsoft.Json.Linq;
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

        public class Save
        {
            public string Name { get; set; }

            public string Download_URL { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory(Globals.docPath);
            Directory.CreateDirectory(Path.Combine(Globals.docPath, "Saves"));
            FetchSaves();
            ReadSettings();
            if (save.SelectedIndex == -1)
            {
                try { save.SelectedIndex = 0; }
                catch { };
            }
        }

        private void FetchSaves()
        {
            var saves = Directory.GetFiles(Path.Combine(Globals.docPath, "Saves"));
            foreach (string saveFile in saves)
            {
                save.Items.Add(Path.GetFileNameWithoutExtension(saveFile));
            }
            try { save.SelectedIndex = 0; }
            catch { };
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
                else if (line.StartsWith("Default language: "))
                {
                    try
                    {
                        language.SelectedIndex = Int32.Parse(line.Split(':')[1].Trim());
                    }
                    catch
                    {
                        MessageBox.Show("Your settings file is corrupt. I will create a new one.", "Error");
                        CreateSettings();
                        return;
                    }
                    i++;
                }
            }
            if (i != 4)
            {
                MessageBox.Show("Your settings file is corrupt. I will create a new one.", "Error");
                CreateSettings();
            }
        }

        private void CreateSettings()
        {
            File.WriteAllText(Path.Combine(Globals.docPath, "settings.ini"), "// Saved settings for the Pikuniku Save Editor. Don't edit this file. Or do. I don't really care. \nDefault resolution: 1920 x 1080 \nDefault fullscreen: true \nDefault save: \nDefault language: ");
            res.SelectedIndex = 1;
            fs.IsChecked = true;
        }

        private void ChangeSettings(string res, string fullscreen, string save, string lang)
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
                else if (line.StartsWith("Default language: "))
                {
                    settings[i] = $"Default language: {lang}";
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
            foreach (string file in Directory.GetFiles(Path.Combine(Globals.docPath, "Saves")))
            {
                File.Delete(file);
            }
            save.Items.Clear();
            using WebClient wc = new WebClient();
            wc.UseDefaultCredentials = true;
            wc.Headers.Add("user-agent", "Pikuniku Save Manager");
            var json = wc.DownloadString("https://api.github.com/repos/Suprnova123/Pikuniku-Save-Manager/contents/?ref=saves");
            JArray saves = JArray.Parse(json);
            IList<JToken> savesList = saves.Children().ToList();
            foreach (JToken file in savesList)
            {
                Save save = file.ToObject<Save>();
                if (save.Name == "README.md")
                {
                    continue;
                }
                wc.DownloadFile(new Uri(save.Download_URL), Path.Combine(Globals.docPath, "Saves", save.Name));
            }
            MessageBox.Show($"Download has completed. Saves have been saved to {Path.Combine(Globals.docPath, "Saves")}.", "Notice");
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
            ChangeSettings(res.Text, fs.IsChecked.ToString(), save.Text, language.SelectedIndex.ToString());
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
                }
                else if (line.StartsWith("\"PlayerLanguage"))
                {
                    regFile[i] = line.Split(':')[0] + $":0000000{language.SelectedIndex}";
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

        private void export_Click(object sender, RoutedEventArgs e)
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "regedit.exe";
            proc.StartInfo.UseShellExecute = false;
            proc = Process.Start("regedit.exe", "/e \"" + Path.Combine(Globals.docPath, "Saves", DateTime.Now.Ticks.ToString()) + ".reg\" \"HKEY_CURRENT_USER\\Software\\Sectordub\\Pikuniku\"");
            proc.WaitForExit();
            MessageBox.Show($"Save has been exported successfully to {Path.Combine(Globals.docPath, "Saves", DateTime.Now.Ticks.ToString()) + ".reg"}.");
        }
    }
}
