using DocumentIngestion.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentIngestion.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentLineItem> DocumentLineItems => Set<DocumentLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Documents Table
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FileName).HasColumnName("file_name").IsRequired();
            entity.Property(e => e.FileType).HasColumnName("file_type").IsRequired();
            entity.Property(e => e.ExtractedDate).HasColumnName("extracted_date");
            
            // Add Index to Reference Number as per specification
            entity.Property(e => e.ReferenceNumber).HasColumnName("reference_number");
            entity.HasIndex(e => e.ReferenceNumber);

            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();

            // Configure One-to-Many Relationship with Cascade Delete
            entity.HasMany(d => d.LineItems)
                  .WithOne()
                  .HasForeignKey(li => li.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure DocumentLineItems Table
        modelBuilder.Entity<DocumentLineItem>(entity =>
        {
            entity.ToTable("document_line_items");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
            entity.Property(e => e.DocumentId).HasColumnName("document_id").IsRequired();
            entity.Property(e => e.Description).HasColumnName("description").IsRequired();
            entity.Property(e => e.Quantity).HasColumnName("quantity").IsRequired();
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price").IsRequired();
            entity.Property(e => e.TotalPrice).HasColumnName("total_price").IsRequired();
        });
    }
}
