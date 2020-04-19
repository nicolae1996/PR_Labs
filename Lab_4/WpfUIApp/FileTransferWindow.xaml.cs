using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Shared.Models;

namespace WpfUIApp
{
    /// <summary>
    /// Interaction logic for FileTransferWindow.xaml
    /// </summary>
    public partial class FileTransferWindow : Window
    {
        public FileTransferWindow()
        {
            InitializeComponent();

            List<File> items = new List<File>();
            items.Add(new File
            {
                Name = "File1",
                ModifiedDate = DateTime.Now,
                Path = "/sc/sdc",
                Size = 344,
                Type = "txt"
            });
            lvFiles.ItemsSource = items;
        }
    }
}
