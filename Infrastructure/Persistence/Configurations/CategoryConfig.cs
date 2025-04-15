using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Persistence.Configurations
{
    public class CategoryConfig : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(c => c.Id);

            builder.HasOne<Category>()
                   .WithMany()
                   .HasForeignKey(c => c.ParentCategoryId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
