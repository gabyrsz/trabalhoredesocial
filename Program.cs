using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;
using TrabalhoFinal.Api.Data;
using TrabalhoFinal.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// ======= CONFIG DB =======
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ======= CONFIG JWT =======
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

// ======= AUTENTICAÇÃO =======
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
})
.AddGoogle(options =>
{
    options.ClientId = "302309741079-2ln5q764m07mddtd3bpvrh1tkatbdj11.apps.googleusercontent.com";
    options.ClientSecret = "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";
    options.Scope.Add("email");
    options.SaveTokens = true;
})
.AddFacebook(options =>
{
    options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
    options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
})
.AddTwitter(options =>
{
    options.ConsumerKey = builder.Configuration["Authentication:Twitter:ConsumerKey"];
    options.ConsumerSecret = builder.Configuration["Authentication:Twitter:ConsumerSecret"];
    options.RetrieveUserDetails = true;
});

// ======= CORS =======
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5174", //  porta usada no Vite
                "http://localhost:5173",
                "http://localhost:3000",
                "https://seusite.com"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

//  JSON OPTIONS =======
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    })
    .AddNewtonsoftJson();

// ======= OUTROS SERVIÇOS =======
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGenNewtonsoftSupport();
builder.Services.AddSignalR();
// Adicione isso após builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();
builder.Services.AddHttpClient();

var app = builder.Build();

//  MIDDLEWARES =======
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

// CORS antes de autenticação
app.UseCors("AllowAll");

//  Autenticação antes de autorização
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MensagensHub>("/hub/mensagens");

app.Run();
