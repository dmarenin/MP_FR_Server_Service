using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;

namespace MP_FR_Command
{
    public class License
    {
        bool active=false;
        string vol_ser_numder="";
        string sign;
        string id;

        string salt1 = "0JjQt9Cy0LXQtNCw0Lsg0LLRgNCw0LMg0LIg0YLQvtGCINC00LXQvdGMINC90LXQvNCw0LvQviwNCtCn0YLQviDQt9C90LDRh9C40YIg0YDRg9GB0YHQutC40Lkg0LHQvtC5INGD0LTQsNC70YvQuSwNCtCd0LDRiCDRgNGD0LrQvtC/0LDRiNC90YvQuSDQsdC+0LkhLi4NCtCX0LXQvNC70Y8g0YLRgNGP0YHQu9Cw0YHRjCDigJQg0LrQsNC6INC90LDRiNC4INCz0YDRg9C00Lg7DQrQodC80LXRiNCw0LvQuNGB0Ywg0LIg0LrRg9GH0YMg0LrQvtC90LgsINC70Y7QtNC4LA0K0Jgg0LfQsNC70L/RiyDRgtGL0YHRj9GH0Lgg0L7RgNGD0LTQuNC5DQrQodC70LjQu9C40YHRjCDQsiDQv9GA0L7RgtGP0LbQvdGL0Lkg0LLQvtC54oCm";

        string salt2 = "0JLQvtGCINGB0LzQtdGA0LrQu9C+0YHRjC4g0JHRi9C70Lgg0LLRgdC1INCz0L7RgtC+0LLRiw0K0JfQsNGD0YLRgNCwINCx0L7QuSDQt9Cw0YLQtdGP0YLRjCDQvdC+0LLRi9C5DQrQmCDQtNC+INC60L7QvdGG0LAg0YHRgtC+0Y/RgtGM4oCmDQrQktC+0YIg0LfQsNGC0YDQtdGJ0LDQu9C4INCx0LDRgNCw0LHQsNC90Ysg4oCUDQrQmCDQvtGC0YHRgtGD0L/QuNC70Lgg0LHRg9GB0YPRgNC80LDQvdGLLg0K0KLQvtCz0LTQsCDRgdGH0LjRgtCw0YLRjCDQvNGLINGB0YLQsNC70Lgg0YDQsNC90YssDQrQotC+0LLQsNGA0LjRidC10Lkg0YHRh9C40YLQsNGC0Ywu";

        public List<string> ads = new List<string>();

        public License()
        {
            vol_ser_numder = GetVolumeSerialNumber();

            active = ReadLicFile();

            AddAd(@"&#0010;&#0049;&#0067;&#0032;&#1042;&#1085;&#1077;&#1076;&#1088;&#1077;&#1085;&#1080;&#1077;&#0032;&#0034;&#1043;&#1050;&#0034;&#1069;&#1085;&#1044;&#1080;&#0032;&#1050;&#1086;&#1085;&#1089;&#1072;&#1083;&#1090;&#0034;");            
        }

        public void AddAd(string Ad)
        {
            ads.Add(Ad);
        }

        public string GetAd()
        {
            string result = "";

            if (ads.Count == 0)
            {
                return result;
            }

            Random rnd = new Random();

            result = ads[rnd.Next(0, ads.Count)];

            return result;
        }

        public void ModifiedData(string Data)
        {
            if (active)
            {
                return;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(Data);
  
            string command = doc.ChildNodes[1].SelectSingleNode(@" / ArmRequest/RequestBody/Command", null).InnerText;

            command = command.Trim();
            if (command == "8")
            {
                if (Data.Contains("</Footer>"))
                {
                    Data = Data.Replace("</Footer>", GetAd() + "</Footer>");
                }
                else if (Data.Contains("<FiscalDocNumber>"))
                {
                    Data = Data.Replace("<FiscalDocNumber>", "<Footer>" + GetAd() + "</Footer>"+ "<FiscalDocNumber>");
                }
                else
                {
                    Data = "";
                }
            }
        }

        public bool ReadLicFile()
        {
            if (String.IsNullOrEmpty(this.vol_ser_numder))
            {
                return false;
            }

            string File = "lic.xml";
            if (!System.IO.File.Exists(File))
            {
                return false;
            }

            XmlDocument doc = new XmlDocument();
          
            string _sign;

            try
            {
                doc.Load(File);

                sign = doc.ChildNodes[0].SelectSingleNode(@"/lic/sign", null).InnerText;
                id = doc.ChildNodes[0].SelectSingleNode(@"/lic/id", null).InnerText;

                sign = sign.Replace(salt1, "");
                sign = sign.Replace(salt2, "");

                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] data = System.Text.Encoding.UTF8.GetBytes(vol_ser_numder + id);
                byte[] hash = md5.ComputeHash(data);
                _sign = Convert.ToBase64String(hash);
            }
            catch (Exception)
            {
                return false;
            }
            if (_sign != sign)
            {
                return false;
            }

            return true;
        }

        public string GetVolumeSerialNumber()
        {
            {
                string drive_letter = Path.GetPathRoot(Environment.SystemDirectory);
                drive_letter = drive_letter.Substring(0, 1) + ":\\";

                uint serial_number = 0;
                uint max_component_length = 0;
                StringBuilder sb_volume_name = new StringBuilder(256);
                UInt32 file_system_flags = new UInt32();
                StringBuilder sb_file_system_name = new StringBuilder(256);

                if (GetVolumeInformation(drive_letter, sb_volume_name,
                    (UInt32)sb_volume_name.Capacity, ref serial_number,
                    ref max_component_length, ref file_system_flags,
                    sb_file_system_name,
                    (UInt32)sb_file_system_name.Capacity) == 0)
                {
                    return "";
                }
                else
                {
                    return serial_number.ToString();
                }
            }
        }

        [DllImport("kernel32.dll")]
        private static extern long GetVolumeInformation(string PathName, StringBuilder VolumeNameBuffer, UInt32 VolumeNameSize, ref UInt32 VolumeSerialNumber, ref UInt32 MaximumComponentLength, ref UInt32 FileSystemFlags, StringBuilder FileSystemNameBuffer, UInt32 FileSystemNameSize);
    }
}