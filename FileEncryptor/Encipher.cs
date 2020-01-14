using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace Encryptor
{

    public static class Encipher
    {    
        public static void GenerateRSAKeyPair(out string publicKey, out string privateKey)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
            publicKey = rsa.ToXmlString(false);
            privateKey = rsa.ToXmlString(true);
        }
        //--------------------ШИФРОВАНИЕ ФАЙЛА----------------------------------
        //plainFilePath - Полный путь к файлу, который будет зашифрован
        //encryptedFilePath - Полный путь к зашифрованному файлу
        //manifestFilePath - Полный путь к сгенерированному файлу манифеста
        //product - название продукта
        //productVersion - версия продукта
        //rsaKey - Ключ RSA, используемый для шифрования одноразового симметричного ключа
        //rsaKeyId Идентификатор ключа RSA для внутреннего индекса
        //---------------------------------------------------------------------
        public static string Encrypt(string plainFilePath, 
            string encryptedFilePath, 
            string manifestFilePath, 
            string rsaKey)
        {
            byte[] signatureKey = GenerateRandom(64);
            byte[] encryptionKey = GenerateRandom(16);
            byte[] encryptionIV = GenerateRandom(16);

            EncryptFile(plainFilePath, encryptedFilePath, encryptionKey, encryptionIV);

            byte[] signature = CalculateSignature(encryptedFilePath, signatureKey);

            CreateManifest(signature, signatureKey, encryptionKey, encryptionIV, rsaKey, manifestFilePath);

            return CreateEncryptionInfoXml(signatureKey, encryptionKey, encryptionIV);
        }

        // ------------------ СОЗДАНИЕ ХМЛ КЛЮЧА ШИФРОВАНИЯ ----------------
        //signatureKey - ключ подписи
        //encryptionKey - АЕС шифрованный ключ
        //encryptionIV - АЕС шифрованный ключ ИВ
        // ------------------------------------------------------------------

        private static string CreateEncryptionInfoXml(byte[] signatureKey, byte[] encryptionKey, byte[] encryptionIV)
        {
            string template = "<EncryptionInfo>" +
                "<AESKeyValue>" +
                "<Key/>" +
                "<IV/>" +
                "</AESKeyValue>" +
                "<HMACSHAKeyValue/>" +
                "</EncryptionInfo>";

            XDocument doc = XDocument.Parse(template);
            doc.Descendants("AESKeyValue").Single().Descendants("Key").Single().Value = Convert.ToBase64String(encryptionKey);
            doc.Descendants("AESKeyValue").Single().Descendants("IV").Single().Value = Convert.ToBase64String(encryptionIV);
            doc.Descendants("HMACSHAKeyValue").Single().Value = Convert.ToBase64String(signatureKey);
            return doc.ToString();
        }

        // ---------------- СОЗДАНИЕ РАНДОМНОГО СПИСКА ---------------
        //length - Длина списка
        // -----------------------------------------------------------
        private static byte[] GenerateRandom(int length)
        {
            byte[] bytes = new byte[length];
            using (RNGCryptoServiceProvider random = new RNGCryptoServiceProvider())
            {
                random.GetBytes(bytes);
            }

            return bytes;
        }

        // -------------------- ШИФРОВАНИЕ САМОГО ФАЙЛА ------------------------
        //plainFilePath - Полный путь к файлу, который будет зашифрован
        //encryptedFilePath - Полный путь к зашифрованному файлу
        //key - АЕС клюс 
        //iv - AES ИВ
        // --------------------------------------------------------------------
        private static void EncryptFile(string plainFilePath, 
            string encryptedFilePath, 
            byte[] key, 
            byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 128;
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using (FileStream plain = File.Open(plainFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (FileStream encrypted = File.Open(encryptedFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        using (CryptoStream cs = new CryptoStream(encrypted, encryptor, CryptoStreamMode.Write))
                        {
                            plain.CopyTo(cs);
                        }
                    }
                }
            }
        }

        // -------------------- ДЕШИФРОВАНИЕ САМОГО ФАЙЛА ------------------------
        //plainFilePath - Полный путь к файлу, который будет зашифрован
        //encryptedFilePath - Полный путь к зашифрованному файлу
        //key - АЕС клюс 
        //iv - AES ИВ
        // --------------------------------------------------------------------
        public static void DecryptFile(string plainFilePath, string encryptedFilePath, byte[] key, byte[] iv)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.KeySize = 128;
                aes.Key = key;
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using (FileStream plain = File.Open(plainFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    using (FileStream encrypted = File.Open(encryptedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (CryptoStream cs = new CryptoStream(plain, decryptor, CryptoStreamMode.Write))
                        {
                            encrypted.CopyTo(cs);
                        }
                    }
                }
            }
        }


        // --------------------- ШИФРОВАНИЕ РСА --------------------------
        //datas - байтовый массив для шифрования
        //keyXml - РСА ключ 
        // ---------------------------------------------------------------

        public static byte[] RSAEncryptBytes(byte[] datas, string keyXml)
        {
            byte[] encrypted = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(keyXml);
                encrypted = rsa.Encrypt(datas, true);
            }

            return encrypted;
        }

        // --------------------- ДЕШИФРОВАНИЕ РСА --------------------------
        //datas - байтовый массив для шифрования
        //keyXml - РСА ключ 
        // ---------------------------------------------------------------
        public static byte[] RSADescryptBytes(byte[] datas, string keyXml)
        {
            byte[] decrypted = null;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048))
            {
                rsa.FromXmlString(keyXml);
                decrypted = rsa.Decrypt(datas, true);
            }

            return decrypted;
        }


        // -------------------------- Рассчитать подпись файла --------------
        // filePath - Полный путь к файлу для расчета подписи
        // key - ключ расчётной подписи
        // ------------------------------------------------------------------

        private static byte[] CalculateSignature(string filePath, byte[] key)
        {
            byte[] sig = null;
            using (HMACSHA256 sha = new HMACSHA256(key))
            {
                using (FileStream f = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    sig = sha.ComputeHash(f);
                }
            }

            return sig;
        }

        // ----Создание файла манифеста зашифрованного пакета, используемого для разбора серверной части ---------
        //signature - Подпись данных
        //signatureKey - Подпись данных ключа
        //encryptionKey - АЕС КЛЮЧ ДЕШИФРАТОРА
        //encryptionIv - АЕС ДЕШИФРАТОР ИВ
        //product - Название продукта
        //productVersion - Версия продукта
        //rsaKey - РСА ключ
        //rsaKeyID РСА ключ ИД
        //manifestFilePath - Путь к файлу выходного манифеста
        // --------------------------------------------------------------------------------------------------
        private static void CreateManifest(byte[] signature, 
            byte[] signatureKey, 
            byte[] encryptionKey, 
            byte[] encryptionIv, 
            string rsaKey,
            string manifestFilePath)
        {
            string template = "<DataInfo>" +
                "<Encrypted>True</Encrypted>" + 
                "<KeyEncryption algorithm='RSA2048'>" + 
                "</KeyEncryption>" + 
                "<DataEncryption algorithm='AES128'>" + 
                "<AESEncryptedKeyValue>" + 
                "<Key/>" + 
                "<IV/>" +
                "</AESEncryptedKeyValue>" +
                "</DataEncryption>" + 
                "<DataSignature algorithm='HMACSHA256'>" + 
                "<Value />" +
                "<EncryptedKey />" + 
                "</DataSignature>" + 
                "</DataInfo>";

            XDocument doc = XDocument.Parse(template);
            doc.Descendants("DataEncryption").Single().Descendants("AESEncryptedKeyValue").Single().Descendants("Key").Single().Value = System.Convert.ToBase64String(RSAEncryptBytes(encryptionKey, rsaKey));
            doc.Descendants("DataEncryption").Single().Descendants("AESEncryptedKeyValue").Single().Descendants("IV").Single().Value = System.Convert.ToBase64String(RSAEncryptBytes(encryptionIv, rsaKey));
            doc.Descendants("DataSignature").Single().Descendants("Value").Single().Value = System.Convert.ToBase64String(signature);
            doc.Descendants("DataSignature").Single().Descendants("EncryptedKey").Single().Value = System.Convert.ToBase64String(RSAEncryptBytes(signatureKey, rsaKey));

            doc.Save(manifestFilePath);
        }
    }
}
