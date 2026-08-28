namespace DanhGiaAPI.DTOs.Auth
{
    public class DangNhapResponseDto
    {
        public string Token { get; set; } = null!;
        public DateTime HetHan { get; set; }
        public NguoiDungInfoDto NguoiDung { get; set; } = null!;
    }
}
