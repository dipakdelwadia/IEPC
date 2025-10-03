namespace WebAPI.Domain.Entities
{
    public class User
    {
        public string ClientId { get; set; }
        public string UserID { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}