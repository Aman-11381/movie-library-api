namespace MovieLibrary.DTOs.Auth
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string? UserName { get; set; }   // optional, fallback to email
    }

}
