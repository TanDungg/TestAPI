namespace AiImageGeneratorApi.Models.DTOs
{
    public class UserRegisterDto
    {
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string HoVaTen { get; set; }
        public string DiaChi { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }
        public string HinhAnh { get; set; }
    }
    public class UserPutDto
    {
        public string HoVaTen { get; set; }
        public string DiaChi { get; set; }
        public string Email { get; set; }
        public string Sdt { get; set; }
        public string HinhAnh { get; set; }
    }
}