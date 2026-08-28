using DanhGiaAPI.Common;
using DanhGiaAPI.Models;
using DanhGiaAPI.Repositories;
using DanhGiaAPI.Repositories.Interfaces;
using DanhGiaAPI.Services;
using DanhGiaAPI.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// wwwroot phải tồn tại TRƯỚC builder.Build() — WebRootFileProvider (dùng bởi
// UseStaticFiles) được chốt lúc Build(), tạo thư mục sau đó (VD trong
// ChuKyService khi upload) sẽ không được middleware nhận ra, luôn trả 404.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "chu-ky"));
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "uploads", "dinh-kem"));

// Add services to the container.

// Thêm CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins", builder =>
    {
        builder.AllowAnyOrigin()  // Cho phép tất cả các origin
               .AllowAnyMethod()  // Cho phép tất cả các method (GET, POST, PUT, DELETE...)
               .AllowAnyHeader(); // Cho phép tất cả các header
    });
    //options.AddPolicy("AllowReact",
    //    policy =>
    //    {
    //        policy.WithOrigins("http://localhost:5173") // Cho phép React truy cập
    //              .AllowAnyHeader()
    //              .AllowAnyMethod();
    //    });
});


// Đăng ký DbContext với Dependency Injection (DI)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbConnectionString")));

// Repository + Unit of Work (module đánh giá bếp ăn — xem
// 02. Phantich/modules/*.md). Repository<T> generic dùng chung, mỗi entity có
// interface riêng để thêm method truy vấn đặc thù khi cần.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IVaiTroRepository, VaiTroRepository>();
builder.Services.AddScoped<IPhongBanRepository, PhongBanRepository>();
builder.Services.AddScoped<INhaThauRepository, NhaThauRepository>();
builder.Services.AddScoped<IBepAnRepository, BepAnRepository>();
builder.Services.AddScoped<IDiaDiemNhaAnRepository, DiaDiemNhaAnRepository>();
builder.Services.AddScoped<IKetQuaDanhGiaRepository, KetQuaDanhGiaRepository>();
builder.Services.AddScoped<INguoiDungRepository, NguoiDungRepository>();
builder.Services.AddScoped<INguoiDungVaiTroRepository, NguoiDungVaiTroRepository>();
builder.Services.AddScoped<IPhienDangNhapRepository, PhienDangNhapRepository>();
builder.Services.AddScoped<IChuKyNguoiDungRepository, ChuKyNguoiDungRepository>();
builder.Services.AddScoped<INhomTieuChiRepository, NhomTieuChiRepository>();
builder.Services.AddScoped<ITieuChiRepository, TieuChiRepository>();
builder.Services.AddScoped<ITepDinhKemRepository, TepDinhKemRepository>();
builder.Services.AddScoped<IMauLuongKyRepository, MauLuongKyRepository>();
builder.Services.AddScoped<IChuKyPhieuRepository, ChuKyPhieuRepository>();
builder.Services.AddScoped<INhatKyChinhSuaRepository, NhatKyChinhSuaRepository>();
builder.Services.AddScoped<IPhieu1KiemTraRepository, Phieu1KiemTraRepository>();
builder.Services.AddScoped<IPhieu1ChiTietRepository, Phieu1ChiTietRepository>();
builder.Services.AddScoped<IPhieu1KetLuanRepository, Phieu1KetLuanRepository>();
builder.Services.AddScoped<IPhieu2DanhGiaRepository, Phieu2DanhGiaRepository>();
builder.Services.AddScoped<IPhieu2TieuChiRepository, Phieu2TieuChiRepository>();
builder.Services.AddScoped<IPhieu2KetQuaRepository, Phieu2KetQuaRepository>();
builder.Services.AddScoped<IPhieu2YKienNhaThauRepository, Phieu2YKienNhaThauRepository>();
builder.Services.AddScoped<IPhieu3BaoCaoRepository, Phieu3BaoCaoRepository>();
builder.Services.AddScoped<IPhieu3Bang1DongRepository, Phieu3Bang1DongRepository>();
builder.Services.AddScoped<IPhieu3Bang2DongRepository, Phieu3Bang2DongRepository>();
builder.Services.AddScoped<IPhieu3Bang2GiaTriRepository, Phieu3Bang2GiaTriRepository>();
builder.Services.AddScoped<IPhieu3YKienNhaThauRepository, Phieu3YKienNhaThauRepository>();
builder.Services.AddScoped<IPhieu4TongHopRepository, Phieu4TongHopRepository>();
builder.Services.AddScoped<IPhieu4NhaThauRepository, Phieu4NhaThauRepository>();
builder.Services.AddScoped<IPhieu4BangRepository, Phieu4BangRepository>();
builder.Services.AddScoped<IPhieu4DongRepository, Phieu4DongRepository>();
builder.Services.AddScoped<IPhieu4GiaTriRepository, Phieu4GiaTriRepository>();

// Đăng ký Service cho module đăng nhập/quản lý tài khoản/danh mục
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<INguoiDungService, NguoiDungService>();
builder.Services.AddScoped<IChuKyService, ChuKyService>();
builder.Services.AddScoped<IVaiTroService, VaiTroService>();
builder.Services.AddScoped<IPhongBanService, PhongBanService>();
builder.Services.AddScoped<IBepAnService, BepAnService>();
builder.Services.AddScoped<INhaThauService, NhaThauService>();
builder.Services.AddScoped<INhomTieuChiService, NhomTieuChiService>();
builder.Services.AddScoped<ITieuChiService, TieuChiService>();

// Đăng ký Service cho hạ tầng dùng chung 4 loại phiếu (Giai đoạn 2 — xem
// 02. Phantich/Features.md + modules/LuongTrinhKy.md)
builder.Services.AddScoped<ISoHieuService, SoHieuService>();
builder.Services.AddScoped<ITepDinhKemService, TepDinhKemService>();
builder.Services.AddScoped<IMauLuongKyService, MauLuongKyService>();
builder.Services.AddScoped<IChuKyPhieuService, ChuKyPhieuService>();
builder.Services.AddScoped<IPhieuNhaThauResolver, PhieuNhaThauResolver>();
builder.Services.AddScoped<INhatKyChinhSuaService, NhatKyChinhSuaService>();

// Đăng ký Service cho Phiếu (1): Kiểm tra VSATTP (Giai đoạn 3 — xem
// modules/Phieu1_KiemTraVSATTP.md)
builder.Services.AddScoped<IPhieu1Service, Phieu1Service>();

// Đăng ký Service cho Phiếu (2): Đánh giá chất lượng dịch vụ suất ăn (Giai đoạn 4 — xem
// modules/Phieu2_DanhGiaSuatAn.md)
builder.Services.AddScoped<IPhieu2Service, Phieu2Service>();

// Đăng ký Service cho Phiếu (3): Báo cáo chất lượng dịch vụ suất ăn theo tháng
// (Giai đoạn 5 — xem modules/Phieu3_BaoCaoThang.md)
builder.Services.AddScoped<IPhieu3Service, Phieu3Service>();

// Đăng ký Service cho Phiếu (4): Bảng tổng hợp đánh giá & phân bổ suất ăn
// (Giai đoạn 6 — xem modules/Phieu4_TongHopPhanBo.md)
builder.Services.AddScoped<IPhieu4Service, Phieu4Service>();

// Xác thực JWT (module Đăng nhập — xem 02. Phantich/modules/DangNhap.md)
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Giữ nguyên tên claim gốc ("sub", "co_quyen_duyet_tk"...) thay vì để
        // JwtSecurityTokenHandler tự remap sang URI chuẩn ClaimTypes.*.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// Policy "DuyetTaiKhoan": người có claim co_quyen_duyet_tk (bất kỳ vai trò nào
// bật VaiTro.CoQuyenDuyetTk = 1) — xem modules/QuanLyTaiKhoan.md.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DuyetTaiKhoan", policy => policy.RequireClaim("co_quyen_duyet_tk", "1"));
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
//app.UseCors("AllowReactApp"); // Áp dụng CORS
app.UseCors("AllowAllOrigins");

// Bắt ApiException (và các exception kế thừa như AuthException) ném ra từ
// Service, trả về đúng status code + message thay vì để lộ stack trace 500.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (ApiException ex)
    {
        context.Response.StatusCode = ex.StatusCode;
        await context.Response.WriteAsJsonAsync(new { message = ex.Message });
    }
});

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseStaticFiles(); // phục vụ ảnh chữ ký đã upload ở wwwroot/uploads/chu-ky

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
