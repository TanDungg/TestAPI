namespace AiImageGeneratorApi.Models.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserRegisterDto User { get; set; }
    }

}
