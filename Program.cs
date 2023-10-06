using addkeyserver.Database;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// appsettings.json의 GameDB값
var AppDbStr = builder.Configuration.GetConnectionString("GameDB");
builder.Services.AddDbContextPool<AppDbContext>(options => options
    .UseMySql(AppDbStr, ServerVersion.AutoDetect(AppDbStr))
    .EnableThreadSafetyChecks(false)
//.LogTo(Console.WriteLine)  // 쿼리 로그 남기기(Console에)
);

// 토큰 인증
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();


// Redis
var redisStr = builder.Configuration.GetConnectionString("GameRedis");
var GameRedisOptions = ConfigurationOptions.Parse(redisStr);
GameRedisOptions.Ssl = false;
GameRedisOptions.AbortOnConnectFail = false;
GameRedisOptions.ConnectRetry = 3;
GameRedisOptions.ConnectTimeout = 5000;
GameRedisOptions.SyncTimeout = 5000;
builder.Services.AddScoped<IDatabase>(cfg =>
{
    var redis = ConnectionMultiplexer.Connect(GameRedisOptions);
    return redis.GetDatabase();
});


// Configure the HTTP request pipeline.
/*
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

*/

var app = builder.Build();
app.UseForwardedHeaders(new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

//Jwt 관련
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run("http://*:5000");
