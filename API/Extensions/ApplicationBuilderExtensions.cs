using API.Middlewares;
using Core.Entities.IdentityEntities;
using Core.Interfaces.TokenValidationInterface;
using Infrastructure.Data.DataBaseContext.IdentityDataBaseContext;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Infrastructure.Data.Repositories.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;

namespace API.Extensions
{
	public static class ApplicationBuilderExtensions
	{
		public static WebApplicationBuilder BuilderExtension(this WebApplicationBuilder builder, IConfiguration _config)
		{
			builder.Services.AddScoped<ExceptionHandlingMiddleware>();
			builder.Services.AddScoped<ITokenService, TokenService>();
			builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AppIdentityDbContext>().AddSignInManager<SignInManager<AppUser>>();
			builder.Services.AddSwaggerGen(c =>
			{
				var securitySchema = new OpenApiSecurityScheme()
				{
					Description = "JWT Auth Bearer Scheme",
					Name = "Authorization",
					In = ParameterLocation.Header,
					Type = SecuritySchemeType.Http,
					Scheme = "bearer",
					Reference = new OpenApiReference()
					{
						Type = ReferenceType.SecurityScheme,
						Id = "Bearer"
					}
				};

				c.AddSecurityDefinition("Bearer", securitySchema);
				var securityRequirement = new OpenApiSecurityRequirement()
				{
					{securitySchema, new[]{"Bearer"} }
				};

				c.AddSecurityRequirement(securityRequirement);
			});
			builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters()
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Token:Key"])),
				ValidIssuer = _config["Token:Issuer"],
				ValidateIssuer = true,
				ValidateAudience = false
			});

			builder.Services.AddCors(options => options.AddPolicy("CorsPolicy", policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

			return builder;
		}

	
	}
}
