using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CadDeliveryChecker
{
    public partial class MainWindow : Window
    {
        private readonly List<FileItem> _allFiles = new();

        private string _selectedFolder = "";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SelectFolder_Click(
            object sender,
            RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "確認するフォルダを選択してください"
            };

            if (dialog.ShowDialog() != true)
                return;

            _selectedFolder = dialog.FolderName;

            FolderPathText.Text = _selectedFolder;

            LoadFiles();

            SearchBox.Focus();
        }

        private void LoadFiles()
        {
            _allFiles.Clear();

            try
            {
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                };

                foreach (string path in Directory.EnumerateFiles(
                             _selectedFolder,
                             "*",
                             options))
                {
                    var file = new FileInfo(path);

                    _allFiles.Add(new FileItem
                    {
                        Name = file.Name,

                        Extension =
                            string.IsNullOrEmpty(file.Extension)
                                ? "(なし)"
                                : file.Extension
                                      .TrimStart('.')
                                      .ToUpperInvariant(),

                        FullPath = file.FullName,

                        RelativeFolder =
                            Path.GetRelativePath(
                                _selectedFolder,
                                file.DirectoryName ?? _selectedFolder),

                        Modified =
                            file.LastWriteTime
                                .ToString("yyyy/MM/dd HH:mm"),

                        Size = FormatSize(file.Length)
                    });
                }

                ApplySearch();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "読み込みエラー",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SearchBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            ApplySearch();
        }

        private void ApplySearch()
        {
            string search =
                SearchBox.Text.Trim();

            List<FileItem> results;

            if (string.IsNullOrWhiteSpace(search))
            {
                results = _allFiles
                    .OrderBy(file => file.Name)
                    .ToList();
            }
            else
            {
                string[] words = search.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries);

                results = _allFiles
                    .Where(file =>
                        words.All(word =>
                            file.Name.Contains(
                                word,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            file.Extension.Contains(
                                word,
                                StringComparison.OrdinalIgnoreCase)

                            ||

                            file.RelativeFolder.Contains(
                                word,
                                StringComparison.OrdinalIgnoreCase)))
                    .OrderBy(file => file.Name)
                    .ToList();
            }

            FileGrid.ItemsSource = results;

            SummaryText.Text =
                $"表示: {results.Count} ファイル / " +
                $"全 {_allFiles.Count} ファイル";
        }

        private void ClearSearch_Click(
            object sender,
            RoutedEventArgs e)
        {
            SearchBox.Clear();

            SearchBox.Focus();
        }

        private void FileGrid_MouseDoubleClick(
            object sender,
            MouseButtonEventArgs e)
        {
            if (FileGrid.SelectedItem is not FileItem file)
                return;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = file.FullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "ファイルを開けませんでした",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
                return $"{bytes / 1024d / 1024 / 1024:F1} GB";

            if (bytes >= 1024L * 1024)
                return $"{bytes / 1024d / 1024:F1} MB";

            if (bytes >= 1024)
                return $"{bytes / 1024d:F1} KB";

            return $"{bytes} B";
        }
    }

    public class FileItem
    {
        public string Name { get; set; } = "";

        public string Extension { get; set; } = "";

        public string RelativeFolder { get; set; } = "";

        public string Modified { get; set; } = "";

        public string Size { get; set; } = "";

        public string FullPath { get; set; } = "";
    }
}