using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.Win32;

namespace FileEncryptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.publicKey = null;

            this.freeEvent = new EventWaitHandle(true, EventResetMode.ManualReset);
        }

        private void Bt_selPlain_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".*",
                Filter = SHIFA.Properties.Resources.All_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectPlain
            };
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                this.tb_plainFilePath.Text = ofd.FileName;
            }
        }

        private void Tb_output_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.tb_output.ScrollToEnd();
        }


        private void Bt_encrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!this.freeEvent.WaitOne(0))
            {
                MessageBox.Show(SHIFA.Properties.Resources.Backend_Busy);
                return;
            }

            if (string.IsNullOrEmpty(publicKey))
            {
                MessageBox.Show(this, SHIFA.Properties.Resources.Error_Need_PublicKey);
                return;
            }

            string plainFilePath = this.tb_plainFilePath.Text;
            string encryptedFilePath = MakePath(plainFilePath, ".encrypted");
            string manifestFilePath = MakePath(plainFilePath, ".manifest.xml");

            this.tb_output.Text = SHIFA.Properties.Resources.Out_msg_start_encryption;

            var t = Task.Factory.StartNew(() =>
            {
                freeEvent.Reset();
                string s = Encryptor.Encipher.Encrypt(plainFilePath,
                    encryptedFilePath,
                    manifestFilePath,
                    this.publicKey);

                freeEvent.Set();
                this.UpdateOutput(this.tb_output, SHIFA.Properties.Resources.Out_msg_encrypt_success + "\r\n" + s,true);
            });
        }

        private void UpdateOutput(TextBox tb, string message, bool append)
        {
            this.Dispatcher.Invoke(new Action(() =>
            {
                string newMessage = append ? tb.Text + "\r\n" + message : message;
                tb.Text = newMessage;
            }), null);
        }

        private string publicKey;

        private string privateKey;
        private string manifestFilePath;

        private void Bt_setting_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow sw = new SettingWindow
            {
                PublicKey = this.publicKey
            };
            Nullable<bool> result = sw.ShowDialog();

            if (result == true)
            {
                this.publicKey = sw.PublicKey;
            }
        }

        private static string MakePath(string plainFilePath, string newSuffix)
        {
            string encryptedFileName = System.IO.Path.GetFileNameWithoutExtension(plainFilePath) + newSuffix;
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(plainFilePath), encryptedFileName);
        }

        private void Mi_genKeyPair_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = SHIFA.Properties.Resources.DialogTitle_CreateKey
            };
            System.Windows.Forms.FolderBrowserDialog fbd = folderBrowserDialog;
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string publicKeyPath = System.IO.Path.Combine(fbd.SelectedPath, "publicKey.xml");
                string privateKeyPath = System.IO.Path.Combine(fbd.SelectedPath, "privateKey.xml");

                Encryptor.Encipher.GenerateRSAKeyPair(out string publicKey, out string privateKey);
                using (StreamWriter sw = File.CreateText(publicKeyPath))
                {
                    sw.Write(publicKey);
                }

                using (StreamWriter sw = File.CreateText(privateKeyPath))
                {
                    sw.Write(privateKey);
                }
            }
        }

        private readonly EventWaitHandle freeEvent;

        private void Mi_switch_Click(object sender, RoutedEventArgs e)
        {
            if (!this.freeEvent.WaitOne(0))
            {
                MessageBox.Show(SHIFA.Properties.Resources.Backend_Busy);
                return;
            }

            if (this.grid_encrypt.Visibility == System.Windows.Visibility.Visible)
            {
                this.grid_encrypt.Visibility = System.Windows.Visibility.Collapsed;
                this.grid_decrypt.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this.grid_encrypt.Visibility = System.Windows.Visibility.Visible;
                this.grid_decrypt.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void Bt_selEncrypted_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".*",
                Filter = SHIFA.Properties.Resources.All_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectPlain
            };
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                this.tb_encryptedFilePath.Text = ofd.FileName;
            }
        }

        private void Bt_decrypt_Click(object sender, RoutedEventArgs e)
        {
            if (!this.freeEvent.WaitOne(0))
            {
                MessageBox.Show(SHIFA.Properties.Resources.Backend_Busy);
                return;
            }

            string rsaKey = this.privateKey;
            string encryptedFile = this.tb_encryptedFilePath.Text;
            string plainFile = MakePath(encryptedFile, ".decrypted");
            string manifestFile = this.manifestFilePath;
            XDocument doc = XDocument.Load(manifestFile);
            XElement aesKeyElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/Key");
            byte[] aesKey = Encryptor.Encipher.RSADescryptBytes(Convert.FromBase64String(aesKeyElement.Value), rsaKey);
            XElement aesIvElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/IV");
            byte[] aesIv = Encryptor.Encipher.RSADescryptBytes(Convert.FromBase64String(aesIvElement.Value), rsaKey);

            this.tb_outputDecrypt.Text = SHIFA.Properties.Resources.Out_msg_start_decryption;
            var t = Task.Factory.StartNew(() =>
            {
                freeEvent.Reset();
                Encryptor.Encipher.DecryptFile(plainFile, encryptedFile, aesKey, aesIv);
                freeEvent.Set();
                this.UpdateOutput(this.tb_outputDecrypt, string.Format(SHIFA.Properties.Resources.Out_msg_decryption_success, plainFile), true);
            });
        }

        private void Bt_settingDecrypt_Click(object sender, RoutedEventArgs e)
        {
            DecryptionSettingWindow dsw = new DecryptionSettingWindow
            {
                Key = this.privateKey,
                ManifestFilePath = this.manifestFilePath
            };
            Nullable<bool> result = dsw.ShowDialog();
            if (result == true)
            {
                this.privateKey = dsw.Key;
                this.manifestFilePath = dsw.ManifestFilePath;
            }
        }

        private void Mi_convertKey_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("не работает!");
            OpenFileDialog ofd = new OpenFileDialog
            {
                
                DefaultExt = ".pem",
                Filter = SHIFA.Properties.Resources.PEM_File_Type,
                Title = SHIFA.Properties.Resources.DialogTitle_SelectPem
            };
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                string pemStr = null;
                using (StreamReader sr = File.OpenText(ofd.FileName))
                {
                    pemStr = sr.ReadToEnd().Trim();
                }

                opensslkey.DecodePEMKey(pemStr, out string publicKey, out string privateKey);

                string publicKeyFile = MakePath(ofd.FileName, ".xml");
                string privateKeyFile = MakePath(ofd.FileName, ".xml");

                if (publicKey != null)
                {
                    using (StreamWriter sw = File.CreateText(publicKeyFile))
                    {
                        sw.Write(publicKey);
                    }
                }
                else
                {
                    MessageBox.Show(SHIFA.Properties.Resources.Error_convertToPublicKey);

                }

                if (privateKey != null)
                {
                    using (StreamWriter sw = File.CreateText(privateKeyFile))
                    {
                        sw.Write(privateKey);
                    }
                }
                else
                {
                    MessageBox.Show(SHIFA.Properties.Resources.Error_convertToPrivateKey);
                }
            }
        }

        private void Tb_outputDecrypt_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.tb_outputDecrypt.ScrollToEnd();
        }
    }
}
