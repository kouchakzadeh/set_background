using System;
using System.Text;
using System.Management;
using Microsoft.Win32;
using System.Net;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
namespace LicenseManager
{
    
    public class LicenseChecker
    {
        private static string perProductGuid;//a key for each DLL must be at leas 16 chars
        private static string encrypt(string plain_text)
        {
            var hardware_id = getHardwareId();
            var ret = Encryptor.Encrypt(plain_text, Encoding.ASCII.GetBytes(perProductGuid.Substring(0,16)), Encoding.Default.GetBytes(hardware_id));
            return ret;
        }
        private static string decrypt(string cipher_text)
        {
            var hardware_id = getHardwareId();
            var plain_text = Encryptor.Decrypt(cipher_text, Encoding.ASCII.GetBytes(perProductGuid.Substring(0, 16)), Encoding.Default.GetBytes(hardware_id));
            return plain_text;
        }
        private static void StoreKeyValSecure(string _key, string val)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            key.CreateSubKey("narabsystem");
            key = key.OpenSubKey("narabsystem", true);
            key.CreateSubKey(perProductGuid);
            key = key.OpenSubKey(perProductGuid, true);
            key.SetValue(encrypt(_key), encrypt(val));
        }
        private static string readKeyValSecure(string _key)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE", true);
            key.CreateSubKey("narabsystem");
            key = key.OpenSubKey("narabsystem", true);
            key.CreateSubKey(perProductGuid);
            key = key.OpenSubKey(perProductGuid, true);
            return decrypt(key.GetValue(encrypt(_key)).ToString());
        }
        private static string getHardwareId()
        {
            var mbs = new ManagementObjectSearcher("Select ProcessorId From Win32_processor");
            ManagementObjectCollection mbsList = mbs.Get();
            string id = "";
            foreach (ManagementObject mo in mbsList)
            {
                id = mo["ProcessorId"].ToString();
                break;
            }
            return id;
        }
        private sealed class Encryptor
        {
            private static SymmetricAlgorithm _cryptoService = new TripleDESCryptoServiceProvider();
            public static string Encrypt(string text, byte[] key, byte[] vector)
            {
                return Transform(text, _cryptoService.CreateEncryptor(key, vector));
            }
            public static string Decrypt(string text, byte[] key, byte[] vector)
            {
                return Transform(text, _cryptoService.CreateDecryptor(key, vector));
            }
            private static string Transform(string text, ICryptoTransform cryptoTransform)
            {
                MemoryStream stream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(stream, cryptoTransform, CryptoStreamMode.Write);

                byte[] input = Encoding.Default.GetBytes(text);

                cryptoStream.Write(input, 0, input.Length);
                cryptoStream.FlushFinalBlock();

                return Encoding.Default.GetString(stream.ToArray());
            }
        }

        public static bool checkLicense(string guid,bool err_message_box=false,bool suc_msg_box=false)
        {
            perProductGuid = guid;
            try
            {
                var start_date = DateTime.Parse(readKeyValSecure("start_date"));
                var end_date = DateTime.Parse(readKeyValSecure("end_date"));
                var last_run_date = DateTime.Parse(readKeyValSecure("last_run_date"));
                StoreKeyValSecure("last_run_date", DateTime.Today.ToString());
                var system_now = DateTime.Today;
                if (system_now < last_run_date)//system date pulled back by user
                {
                    if (err_message_box)
                        MessageBox.Show("License Expired, Please renew your license.");
                    return false;
                }
                if (system_now > end_date)//license expired
                {
                    if (err_message_box)
                        MessageBox.Show("License Expired, Please renew your license.");
                    return false;
                }
                if(suc_msg_box)
                    MessageBox.Show("License Valid, until " + end_date.ToString());
                return true;
            }
            catch(Exception)
            {
                if (err_message_box)
                    MessageBox.Show("License Expired, Please review your license.");
                return false;
            }
        }
    }
}
