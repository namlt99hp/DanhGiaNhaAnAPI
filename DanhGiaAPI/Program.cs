using DanhGiaAPI.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
//app.UseCors("AllowReactApp"); // Áp dụng CORS
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
