namespace MovieLibrary.Entities
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string Token { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public DateTime? RevokedAt { get; set; }
        public string? ReplacedByToken { get; set; }
    }

}
