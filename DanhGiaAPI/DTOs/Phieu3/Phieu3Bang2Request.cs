namespace DanhGiaAPI.DTOs.Phieu3
{
    public class Phieu3Bang2GiaTriRequest
    {
        // TC1..TC6
        public string MaTieuChi { get; set; } = string.Empty;
        public decimal? GiaTri { get; set; }
    }

    public class Phieu3Bang2DongRequest
    {
        public int PhongBanId { get; set; }
        public List<Phieu3Bang2GiaTriRequest> GiaTri { get; set; } = new();
    }
}
