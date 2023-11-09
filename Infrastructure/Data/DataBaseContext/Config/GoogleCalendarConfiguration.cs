using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data.DataBaseContext.Config
{
	public class GoogleCalendarConfiguration : IEntityTypeConfiguration<GoogleCalendarPayload>
	{
		public void Configure(EntityTypeBuilder<GoogleCalendarPayload> builder)
		{
			builder.HasNoKey();
			builder.Property(p=> p.Summary).IsRequired().HasMaxLength(120);
			builder.Property(p => p.Description).IsRequired().HasMaxLength(120);
			builder.Property(p => p.Start).IsRequired().HasMaxLength(120);
			builder.Property(p => p.End).IsRequired().HasMaxLength(120);
			builder.Property(p => p.Location).IsRequired().HasMaxLength(120);
		}
	}
}
