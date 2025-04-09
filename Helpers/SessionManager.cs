using Meditrans.Client.Models;

namespace Meditrans.Client.Helpers
{
    public static class SessionManager
    {
        public static string Token { get; set; }
        public static string Username { get; set; }
        public static Guid UserId { get; set; }
        public static UserRole Role { get; set; }
    }
}
