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

    public partial class SettingWindow : Window, INotifyPropertyChanged
    {

        public SettingWindow()
        {
            InitializeComponent();

            this.DataContext = this;
        }

        private string publicKey;
        private string product;
        private string productVersion;
        private string publicKeyID;

        public string PublicKey
        {
            get
            {
                return this.publicKey;
            }
            set
            {
                if (value != this.publicKey)
                {
                    this.publicKey = value;
                    Notify("PublicKey");
                }
            }
        }

        public string Product
        {
            get
            {
                return this.product;
            }
            set
            {
                if (value != this.product)
                {
                    this.product = value;
                    Notify("Product");
                }
            }
        }

        public string ProductVersion
        {
            get
            {
                return this.productVersion;
            }
            set
            {
                if (value != this.productVersion)
                {
                    this.productVersion = value;
                    Notify("ProductVersion");
                }
            }
        }

        public string PublicKeyID
        {
            get
            {
                return this.publicKeyID;
            }
            set
            {
                if (value != this.publicKeyID)
                {
                    this.publicKeyID = value;
                    Notify("PublicKeyID");
                }
            }
        }

        private void Bt_OK_Click(object sender, RoutedEventArgs e)
        {

            this.DialogResult = true;

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        void Notify(string propName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
        #endregion

        private void Bt_importPublicKey_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".xml",
                Filter = SHIFA.Properties.Resources.XML_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectPublicKey
            };
            Nullable<bool> result = ofd.ShowDialog(this);
            if (result == true)
            {
                using (StreamReader sr = File.OpenText(ofd.FileName))
                {
                    this.PublicKey = sr.ReadToEnd();
                }
            }
        }
    }
}
