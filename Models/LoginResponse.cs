﻿namespace Meditrans.Client.Models
{
    public enum UserRole
    {
        Admin,
        Driver,
        Client,
        User
    }
    public class LoginResponse
    {
        public bool IsSuccess { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }

        public string UserId { get; set; }

        public String Username { get; set; }
        //public Guid Role { get; set; }
        //public UserRole Role { get; set; }

    }
}
