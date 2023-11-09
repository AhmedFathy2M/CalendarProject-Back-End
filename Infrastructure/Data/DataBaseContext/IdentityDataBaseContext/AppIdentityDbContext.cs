using Core.Entities;
using Core.Entities.IdentityEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.DataBaseContext.IdentityDataBaseContext
{
	public class AppIdentityDbContext : IdentityDbContext<AppUser>
	{
		public AppIdentityDbContext(DbContextOptions<AppIdentityDbContext> options) : base(options)
		{
		}
		protected override void OnModelCreating(ModelBuilder builder)
		{
			builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
			base.OnModelCreating(builder);
		}
	}
}
