using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<Context>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// be able to inject JWTService class inside our contorllers
builder.Services.AddScoped<JWTService>();

//defining our IdentityCore Service
builder.Services.AddIdentityCore<User>(options =>
{
    //password configuration
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    // for email confimation
    options.SignIn.RequireConfirmedEmail = true;
})
    .AddRoles<IdentityRole>() //be able to add roles
    .AddRoleManager<RoleManager<IdentityRole>>()// be able to make sue of RoleManager
    .AddEntityFrameworkStores<Context>()// providing our context
    .AddSignInManager<SignInManager<User>>() // make use of Signin manager
    .AddUserManager<UserManager<User>>() //make use of UserManager to create users
    .AddDefaultTokenProviders(); // be able to create tokens for email confirmation

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        //validate te token based on the key we have provided inside appsettngs.development.json JWT:Key
        ValidateIssuerSigningKey = true,
        // the issuer signing key based on the JWTKey
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"])),
        // the issuer which in here is the api project url we are using
        ValidIssuer = builder.Configuration["JWT:Issuer"],
        //validate the iisuer (who ever is issuing the JWT)
        ValidateIssuer = true,
        // dont validate audience (angular side)
        ValidateAudience = false
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//adding UseAuthentication into our pipeline and this should come before UseAuthorizaton
//Authetication verifies the identity of a user or service, and authorization determines their access rigt.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
