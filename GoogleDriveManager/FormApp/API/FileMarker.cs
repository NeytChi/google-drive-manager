namespace GoogleDriveManager
{
    public class FileMarker
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public FileMarker()
        {

        }
        public FileMarker(string name, string type)
        {
            this.Name = name;
            this.Type = type;
        }
    }


}
