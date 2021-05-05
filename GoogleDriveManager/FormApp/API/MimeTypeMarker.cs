namespace GoogleDriveManager
{
    /// <summary>
    /// Класс с данными, для обозначения типа файла по протоколу HTTP
    /// </summary>
    public class MimeTypeMarker
    {
        public string Extension { get; set; }

        public string MimeType { get; set; }
        public string ConverterType { get; set; }
        public MimeTypeMarker()
        {

        }
        public MimeTypeMarker(string extension, string type, string converter)
        {
            Extension = extension;
            MimeType = type;
            ConverterType = converter;
        }
    }
}
