namespace GoogleDriveManager
{
    /// <summary>
    /// Класс для обозначения файлов, которые находятся на локальной машине.
    /// </summary>
    public class LocalFile
    {
        public string Path { get; set; }
        public string Name { get; }

        public LocalFile(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
    }
}
