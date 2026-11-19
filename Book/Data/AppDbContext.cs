using Book.Models;
using Microsoft.EntityFrameworkCore;

namespace Book.Data;

/// <summary>
/// 書本管理系統 DbContext。
/// 目前只對應分類、書籍、借閱紀錄；使用者資料等導入 Identity 後再加入。
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookItem> Books => Set<BookItem>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // 分類：名稱唯一
        b.Entity<Category>(e =>
        {
            e.ToTable("Categories");
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });

        // 書籍：庫存不可為負（對應 UPDATE ... WHERE Quantity > 0）
        b.Entity<BookItem>(e =>
        {
            e.ToTable("Books", t => t.HasCheckConstraint("CK_Books_Quantity", "[Quantity] >= 0"));
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Author).HasMaxLength(100).IsRequired();
            e.Property(x => x.Publisher).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Title);

            // 分類有書時不可刪除分類
            e.HasOne<Category>().WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // 借閱紀錄：刪書時連同歷史紀錄一併刪除（未歸還的由業務規則先擋下）
        // 狀態（剩 N 天 / 逾期）不存欄位，顯示時由 BorrowStatus 計算
        b.Entity<BorrowRecord>(e =>
        {
            e.ToTable("BorrowRecords");
            e.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            e.Property(x => x.BorrowedAt).HasColumnType("datetime2(0)");
            e.Property(x => x.DueDate).HasColumnType("date");
            e.Property(x => x.ReturnedAt).HasColumnType("datetime2(0)");
            e.HasIndex(x => new { x.UserId, x.ReturnedAt });

            e.HasOne<BookItem>().WithMany()
                .HasForeignKey(x => x.BookId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
