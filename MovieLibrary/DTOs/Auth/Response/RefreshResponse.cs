namespace MovieLibrary.DTOs.Auth.Response
{
    public class RefreshResponse
    {
        public string Token { get; set; }
        public string ExpiresInMinutes { get; set; }
    }
}
