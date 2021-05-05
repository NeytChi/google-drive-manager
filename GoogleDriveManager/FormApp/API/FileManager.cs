using Ionic.Zip;
using Serilog;
using System;
using System.IO;
using System.Text;
using System.Linq;

namespace GoogleDriveManager
{
    public class FileManager
    {
        private readonly ILogger Logger = OperateLoggerFactory.Get();
        /// <summary>
        /// Ф-ия для архивации файлов в Zip архив.
        /// </summary>
        /// <param name="path">Путь к файлу для архивации</param>
        /// <returns></returns>
        public string CompressFile(string path)
        {
            var zipPath = path.Split('.').First() + ".zip";
            try
            {
                using (var zip = new ZipFile(Encoding.UTF8))
                {
                    if (Path.HasExtension(path))
                    {
                        zip.AddFile(path);
                    }
                    else
                    {
                        zip.AddDirectory(path);
                    }
                    zip.Save(zipPath);
                }
                return zipPath;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return null;
            }
        }
        public bool CreateFile(string saveFile, string contentToWrite)
        {
            try
            {
                using (var fs = File.Create(saveFile))
                {
                    for (byte i = 0; i < 100; i++)
                    {
                        fs.WriteByte(i);
                    }
                }
                File.WriteAllText(saveFile, contentToWrite);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }
        public bool WriteToFile(string saveFile, string contentToWrite)
        {
            try
            {
                using (StreamWriter w = File.AppendText(saveFile))
                {
                    w.WriteLine(contentToWrite);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }
        }
        /// <summary>
        /// Ф-ия для создания хеш-последовательности.
        /// </summary>
        /// <param name="filePath">Путь к файлу, от которого будет создан хеш.</param>
        /// <returns></returns>
        public string HashGenerator(string filePath)
        {
            if (Path.HasExtension(filePath))
            {
                try
                {
                    using (var md5 = System.Security.Cryptography.MD5.Create())
                    {
                        using (var stream = File.OpenRead(filePath))
                        {
                            return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", null).ToLower();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    return null;
                }

           }
           return null;
        }
        public void DeleteCredFile(string savePath, string userName)
        {
            string[] files = Directory.GetFiles(Path.Combine(savePath, ".credentials"));
            foreach (string file in files)
            {
                if (userName == file.Split('-').Last())
                {
                    File.Delete(file);
                }
            }
        }
        public string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
        }
    }   
}
