using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GR.Core.Extensions;
using Microsoft.Win32;
using File = Shared.Models.File;

namespace WpfUIApp
{
    /// <summary>
    /// Interaction logic for FileTransferWindow.xaml
    /// </summary>
    public partial class FileTransferWindow : Window
    {
        /// <summary>
        /// Data
        /// </summary>
        private ObservableCollection<File> _collection;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileTransferWindow()
        {
            InitializeComponent();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            try
            {
                var items = new List<File>();
                var filesRequest = await App.Client.GetFilesAsync();
                if (filesRequest.Success && filesRequest.Value != null)
                {
                    items.AddRange(filesRequest.Value);
                }

                _collection = items.ToObservableCollection();

                lvFiles.ItemsSource = _collection;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        /// <summary>
        /// On upload item click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UIElement_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                DefaultExt = ".txt",
                Filter = "Text documents (.txt)|*.txt"
            };

            if (fileDialog.ShowDialog() ?? false)
            {
                var info = new FileInfo(fileDialog.FileName);
                var data = System.IO.File.ReadAllBytes(fileDialog.FileName);
                var uploadResult = await App.Client.UploadFileAsync(info.Name, data);
                if (uploadResult.Success)
                {
                    _collection.Add(new File
                    {
                        Name = info.Name,
                        Path = "",
                        ModifiedDate = info.LastAccessTime,
                        Type = info.Extension,
                        Size = info.Length
                    });
                    MessageBox.Show("File was uploaded");
                }
                else
                {
                    MessageBox.Show(uploadResult.Error);
                }
            }
            else
            {
                MessageBox.Show("File not selected");
            }
        }

        /// <summary>
        /// Download selected files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Download_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var req = new List<File>();
            var selected = lvFiles.SelectedItems;
            foreach (var el in selected)
            {
                var t = el.GetType();
                req.Add(el as File);
            }

            var downFiles = await App.Client.DownloadFileAsync(req);
            if (downFiles.Success)
            {
                var downPath = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders", "{374DE290-123F-4565-9164-39C4925E467B}", string.Empty)?.ToString();
                foreach (var f in downFiles.Value.ToList())
                {
                    var newFPath = Path.Combine(downPath, f.Name);
                    await using var fs = new FileStream(newFPath, FileMode.Create, FileAccess.Write);
                    fs.Write(f.Blob);
                }
                MessageBox.Show("File are downloaded, check in your download folder");
            }
            else
            {
                MessageBox.Show("Error to download files");
            }
        }
    }
}
