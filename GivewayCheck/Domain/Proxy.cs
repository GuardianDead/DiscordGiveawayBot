namespace GivewayCheck.Domain
{
    public class Proxy
    {
        public string Address { get; set; }
        public int Port { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        public Proxy(string address, int port, string login, string password)
        {
            Address = address;
            Port = port;
            Login = login;
            Password = password;
        }
    }
}
