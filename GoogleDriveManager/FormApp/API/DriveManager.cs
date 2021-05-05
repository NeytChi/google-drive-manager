using Google.Apis.Auth.OAuth2;
using Google.Apis.Download;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using GoogleDriveManager.API;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace GoogleDriveManager
{
    public class DriveManager
    {
        delegate void UpdateStatusBar(long bytes, string message);
        private readonly UpdateStatusBar UpdateMainForm;
        private readonly ILogger Logger = OperateLoggerFactory.Get();
        private readonly FileManager FileManager = new FileManager();

        private readonly string[] Scopes = { DriveService.Scope.Drive };

        private UserCredential credential;
        private DriveService DriveService;

        private readonly string appDataSavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) 
            + "\\BackUpManager";
        private readonly string applicationName = "Testing";

        
        private readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public DriveManager(MainForm mainForm)
        {
            UpdateMainForm = mainForm.UpdateStatusBar;
        }
        public bool GoogleDriveConnection(string jsonSecretPath, string userName)
        {
            return GetCredential(jsonSecretPath, userName) && CreateDriveService();
        }

        public List<DriveFile> CreateDriveFiles(string fileName = null, string fileType = null)
        {
            try
            {
                var fileList = new List<DriveFile>();
                if (fileName == null && fileType == null)
                {
                    var listRequest = DriveService.Files.List();
                    listRequest.PageSize = 1000;
                    listRequest.Fields = "nextPageToken, files(mimeType, id, name, parents, size, modifiedTime, md5Checksum, webViewLink)";
                    var files = listRequest.Execute().Files;
                    fileList.Clear();
                    if (files != null && files.Count > 0)
                    {
                        fileList = SetListFile(files);
                    }
                    return fileList;
                }
                return CreateDriveFilesByFilter(fileName, fileType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
            return null;
        }
        private List<DriveFile> CreateDriveFilesByFilter(string fileName, string fileType)
        {
            string pageToken = null;
            var fileList = new List<DriveFile>();
            do
            {
                var request = DriveService.Files.List();
                request.PageSize = 1000;
                request.Q = "name contains '" + fileName + "'";
                if (fileType != null)
                {
                    request.Q += "and (mimeType contains '" + fileType + "')";
                }
                request.Spaces = "drive";
                request.Fields = "nextPageToken, files(mimeType, id, name, parents, size, modifiedTime, md5Checksum, webViewLink)";
                request.PageToken = pageToken;
                var result = request.Execute();
                fileList = SetListFile(result.Files);
                pageToken = result.NextPageToken;
            }
            while (pageToken != null);
            return fileList;
        }
        
        private List<DriveFile> SetListFile(IList<Google.Apis.Drive.v3.Data.File> files)
        {
            var fileList = new List<DriveFile>();
            foreach (var file in files)
            {

                fileList.Add(new DriveFile
                {
                    Name = file.Name,
                    Size = SizeFix(file.Size.ToString(), file.MimeType),
                    LastModified = file.ModifiedTime.ToString(),
                    Type = file.MimeType,
                    Id = file.Id,
                    Hash = file.Md5Checksum,
                    WebContentLink = file.WebViewLink
                });
            }
            return fileList;
        }

        private string SizeFix(string bytesString, string type, int decimalPlaces = 1)
        {
            long value;
            if (long.TryParse(bytesString, out value))
            {
                if (value < 0)
                {
                    return "-" + SizeFix((-value).ToString(), type);
                }

                int i = 0;
                decimal dValue = (decimal)value;
                while (Math.Round(dValue, decimalPlaces) >= 1000)
                {
                    dValue /= 1024;
                    i++;
                }
                return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
            }
            return type.Split('.').Last();
        }
        /// <summary>
        /// Авторизация пользователя на сервисе Google.
        /// </summary>
        /// <param name="clientSecretPath">Секретный ключ от приложения, от имени которого будет происходить работа с API</param>
        /// <param name="userName">Имя пользователя, который будет авторизирован</param>
        /// <returns>Ответ, удачен ли запрос.</returns>
        private bool GetCredential(string clientSecretPath, string userName)
        {
            string savePath = Path.Combine(appDataSavePath, Path.GetFileName(clientSecretPath));
            if (File.Exists(savePath))
            {
                try
                {
                    using (var stream = new FileStream(savePath, FileMode.Open, FileAccess.Read))
                    {
                        var credPath = Path.Combine(appDataSavePath, ".credentials");

                        credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                            GoogleClientSecrets.Load(stream).Secrets, Scopes, "Drive-" + userName, CancellationToken.None,
                            new FileDataStore(credPath, true)).Result;
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                    return false;
                }
            }
            File.Copy(clientSecretPath, Path.Combine(appDataSavePath, Path.GetFileName(clientSecretPath)));
            return GetCredential(clientSecretPath, userName);
        }
        /// <summary>
        /// Создание екземпляра класса DriveService, для работы с API Google Drive.
        /// </summary>
        /// <returns></returns>
        private bool CreateDriveService()
        {
            try
            {
                DriveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = applicationName,
                });
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }

        }
        /// <summary>
        /// Ф-ия для отправки файла на диск Google Drive.
        /// </summary>
        /// <param name="folderId">Идентификатор папки, в которую будет отправлен файл.</param>
        /// <param name="fileName">Имя файла на отправку.</param>
        /// <param name="filePath">Путь к файлу на отправку.</param>
        /// <param name="fileType">Тип файла на отправку.</param>
        /// <returns>Ответ, загружен ли файл</returns>
        private bool UploadFileToDrive(string folderId, string fileName, string filePath, string fileType)
        {
            // Изменение статуса загрузки
            UpdateMainForm(0, "Uploading...");
            long totalSize = 100000;
            // Получаем информацию файла по его пути 
            var fi = new FileInfo(filePath);
            totalSize = fi.Length;
            // Создаем ексземпляр файла, который будет отправлен на сервер
            var fileMetadata = new Google.Apis.Drive.v3.Data.File() { Name = fileName};
            // Проверяем номер папки в которую будет помещен файл
            if (folderId != null) {
                // Добавляем номер папки в список файла, который будет загружен, тем самым указываем где файл 
                // будет находится после загрузки
                fileMetadata.Parents = new List<string>{ folderId };
            }
            // Считываем байты файла, который будет загружен
            using (var stream = new FileStream(filePath, FileMode.Open)) {
                // Создаем екземпляр запроса на оправку файла на сервер
                var request = DriveService.Files.Create(fileMetadata, stream, fileType);
                // Устанавливаем размер отправляемых пакетов, по дефолту
                request.ChunkSize = ResumableUpload.MinimumChunkSize;
                // Устанавливаем функции обновления статуса загрузки
                request.ProgressChanged += (IUploadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case UploadStatus.Uploading: { UpdateMainForm(progress.BytesSent * 100 / totalSize, "Uploading..."); break;}
                        case UploadStatus.Completed: { UpdateMainForm(100, "Upload complete."); break; }
                        case UploadStatus.Failed: { UpdateMainForm(0, "Upload failed."); break; }
                    }
                };
                request.Upload();
            }
            return true;
        }

        private bool UploadFileToDrive(string folderId, string fileName, string filePath, string fileType, bool onlyNew)
        {
            if (onlyNew)
            {
                if (!CompareHash(FileManager.HashGenerator(filePath)))
                {
                    UploadFileToDrive(folderId, fileName, filePath, fileType);
                    return true;
                }
                return false;

            }
            UploadFileToDrive(folderId, fileName, filePath, fileType);
            return true;    
        }


        public bool CompareHash(string hashToCompare)
        {
            foreach (var file in CreateDriveFiles())
            {
                if (file.Hash == hashToCompare) 
                    return true;
            }
            return false;
        }
        /// <summary>
        /// Функция создания папки на Google Drive диске.
        /// </summary>
        /// <param name="folderName">Имя папки, которая будет создана</param>
        /// <param name="parentId">Идентификатор папки, в которой будет создана папка.</param>
        /// <returns></returns>
        public string CreateFolderToDrive(string folderName, string parentId)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = folderName,
                MimeType = "application/vnd.google-apps.folder"
            };
            if (parentId != null)
            {
                fileMetadata.Parents = new List<string> { parentId };
            }
            try
            {
                var request = DriveService.Files.Create(fileMetadata);
                request.Fields = "id";
                var file = request.Execute();
                return file.Id;
            }
            catch(Exception ex)
            {
                Logger.Error(ex.Message);
                return null;
            }
        }


        public void UploadToDrive(string path, string name, string parentId, bool onlyNew)
        {
            if (Path.HasExtension(path))
            {
                UploadFileToDrive(parentId, name, path, GetMimeType(Path.GetFileName(path)), onlyNew);
                return;
            }
            DirectoryUpload(path, parentId, onlyNew);    
        }

        public void DirectoryUpload(string path, string parentId, bool onlyNew)
        {
            try
            {
                string folderId = CreateFolderToDrive(Path.GetFileName(path), parentId);

                DirectoryInfo dir = new DirectoryInfo(path);
                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException(
                        "Source directory does not exist or could not be found: "
                        + path);
                }
                
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                {
                    UploadFileToDrive(
                        folderId, file.Name,
                        Path.Combine(path, file.Name),
                        GetMimeType(file.Name), onlyNew);
                }

                DirectoryInfo[] dirs = dir.GetDirectories();
                foreach (DirectoryInfo subdir in dirs)
                {
                    DirectoryUpload(subdir.FullName,  folderId, onlyNew);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void ConvertMemoryStreamToFileStream(MemoryStream stream, string savePath)
        {
            FileStream fileStream;
            using (fileStream = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                try
                {
                    stream.WriteTo(fileStream);
                    fileStream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(ex.Message);
                }
            }
        }

        public void RemoveFile(string fileID)
        {
            var request = DriveService.Files.Delete(fileID);
            request.Execute();
        }

        public void DownloadFromDrive(string filename, string fileId, string savePath, string mimeType)
        {
            long totalSize = 100000;
            UpdateMainForm(0, "Downloading...");
            try
            {
                if (Path.HasExtension(filename))
                {
                    DownloadFile(mimeType, fileId, savePath, filename, totalSize);
                    return;
                }  
                DownloadFolder(mimeType, fileId, savePath, filename);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }
        private void DownloadFile(string mimeType, string fileId, string savePath, string fileName, long totalSize)
        {
            var request = DriveService.Files.Get(fileId);
            var stream = new MemoryStream();
            request.MediaDownloader.ProgressChanged +=
                (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading:{ UpdateMainForm(progress.BytesDownloaded * 100 / totalSize, "Downloading..."); break; }
                        case DownloadStatus.Completed: { UpdateMainForm(100, "Download complete."); break; }
                        case DownloadStatus.Failed: { UpdateMainForm(0, "Download failed."); break; }
                    }
                };
            request.Download(stream);
            ConvertMemoryStreamToFileStream(stream, savePath + @"\" + fileName);
            stream.Dispose();
        }
        /// <summary>
        /// Ф-ия для скачивания папок с Google Drive диска.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <param name="fileId"></param>
        /// <param name="savePath"></param>
        /// <param name="fileName"></param>
        private void DownloadFolder(string mimeType, string fileId, string savePath, string fileName)
        {
            string extension = "", converter = "";
            foreach (var obj in MimeTypes.GetList())
            {
                if (mimeType == obj.MimeType)
                {
                    extension = obj.Extension;
                    converter = obj.ConverterType;
                }
            }
            var request = DriveService.Files.Export(fileId, converter);
            var stream = new MemoryStream();
            request.MediaDownloader.ProgressChanged += (IDownloadProgress progress) =>
                {
                    switch (progress.Status)
                    {
                        case DownloadStatus.Downloading: { break; }
                        case DownloadStatus.Completed: { UpdateMainForm(100, "Download Complete!"); break; }
                        case DownloadStatus.Failed: 
                            {
                                UpdateMainForm(0, "Download failed.");
                                MessageBox.Show("File failed to download!!!", "Download Message", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                            }
                    }
                };
            request.Download(stream);
            ConvertMemoryStreamToFileStream(stream, savePath + @"\" + fileName + extension);
            stream.Dispose();
        }
        private string GetMimeType(string fileName)
        {
            string mimeType = "application/unknown";
            string ext = Path.GetExtension(fileName).ToLower();
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);

            if (regKey != null && regKey.GetValue("Content Type") != null)
                mimeType = regKey.GetValue("Content Type").ToString();
            
            return mimeType;
        }
    }
}
