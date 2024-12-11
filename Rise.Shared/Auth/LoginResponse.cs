namespace Rise.Shared.Auth
{
    public class LoginResponse
    {
        public required TokenDTO tokenDTO { get; set; }
        public required UserInfo User { get; set; }
        public class UserInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
        }
    }
}
