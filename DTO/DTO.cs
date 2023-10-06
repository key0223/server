namespace addkeyserver.DTO
{
    public class DTO
    {
        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
        public class LoginResponse
        {
            public string? Token { get; set; }
            public string Message { get; set; }
            public PlayerInfo Player { get; set; }
        }

        public class RegisterRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
            public string PlayerName { get; set; }
        }
        public class RegisterResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; }
        }

        public class PlayerInfo
        {
            public int PlayerDbId { get; set; }
            public string PlayerName { get; set; }

            public string? Token { get; set; }
            public int Gold { get; set; }
            public int Gem { get; set; }
            public int Health { get; set; }
        }

        public class RedisPlayerInfo
        {
            public int UserDbId { get; set; }
        }
    }
}
