using System.Text.Json.Serialization;
using API.Extensions;
using API.Middlewares;
using Core.Interfaces;
using Infrastructure.Data.DataBaseContext.IdentityDataBaseContext;
using Infrastructure.Data.Repositories.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddDbContext<AppIdentityDbContext>(x =>
                x.UseNpgsql(builder.Configuration.GetConnectionString("IdentityConnection")));
            
            builder.BuilderExtension(builder.Configuration);
            
            builder.Services.AddScoped<ICalendarService, CalendarService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();
            app.UseAuthentication();
            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.MapControllers();

            app.Run();
        }
    }
}