using Meditrans.Client.Models;

namespace Meditrans.Client.Helpers
{
    public static class SessionManager
    {
        public static string Token { get; set; }
        public static string Username { get; set; }
        public static string UserId { get; set; }
        public static string Role { get; set; }
    }
}
