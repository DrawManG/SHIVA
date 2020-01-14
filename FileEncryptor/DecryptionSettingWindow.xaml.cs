using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace FileEncryptor
{
    public partial class DecryptionSettingWindow : Window, INotifyPropertyChanged
    {
        public DecryptionSettingWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion

        private string key;
        private string manifestFilePath;

        public string Key
        {
            get
            {
                return this.key;
            }
            set
            {
                if (value != this.key)
                {
                    this.key = value;
                    Notify("Key");
                }
            }
        }

        public string ManifestFilePath
        {
            get
            {
                return this.manifestFilePath;
            }
            set
            {
                if (value != this.manifestFilePath)
                {
                    this.manifestFilePath = value;
                    Notify("ManifestFilePath");
                }
            }
        }

        private void Bt_selManifest_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = SHIFA.Properties.Resources.XML_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectManifest
            };
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                this.ManifestFilePath = ofd.FileName;
            }
        }

        private void Bt_importDescryptKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = SHIFA.Properties.Resources.XML_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectKey
            };
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                using (StreamReader sr = File.OpenText(ofd.FileName))
                {
                    this.Key = sr.ReadToEnd();
                }
            }
        }

        private void Bt_OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
