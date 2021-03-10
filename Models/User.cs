namespace Arco.Models
{
    public class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public bool Isadmin { get; set; }

        public UserConfig UserConfig { get; set; }
    }
}
