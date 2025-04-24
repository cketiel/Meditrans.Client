﻿using Meditrans.Client.Models;
using Newtonsoft.Json.Linq;

namespace Meditrans.Client.Helpers
{
    public static class SessionManager
    {
        public static string Token { get; set; }
        public static string Username { get; set; }
        public static string UserId { get; set; }
        public static string Role { get; set; }

        public static bool IsAuthenticated => !string.IsNullOrEmpty(Token);
        public static void Clear()
        {
            Token = null;
            Username = null;
            UserId = null;
            Role = null;
        }
    }
}
