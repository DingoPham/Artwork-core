namespace ArtworkCore.Class
{
    public class LoginRequest
    {
        internal readonly bool RememberMe;

        public string UserName { get; set; }
        public string Password { get; set; }
        public string Role {  get; set; }
    }
}
