namespace GoogleDriveManager
{
    /// <summary>
    /// Класс с данными, для обозначения аккаунта пользователя.
    /// </summary>
    public class User
    {
        public string UserName { get; set; }
        public string ClientSecretPath { get; set; }

        public User(string name, string path)
        {
            UserName = name;
            ClientSecretPath = path;
        }
    }
}
