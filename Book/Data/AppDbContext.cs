using Microsoft.EntityFrameworkCore;

namespace Book.Data;

/// <summary>
/// 書本管理系統 DbContext。
/// 目前只對應分類、書籍、借閱紀錄；使用者資料等導入 Identity 後再加入。
/// 欄位、索引、外鍵以 Data Annotations 寫在各 Model；這裡只放 Annotations 表達不了的規則。
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<BookItem> Books => Set<BookItem>();
    public DbSet<BorrowRecord> BorrowRecords => Set<BorrowRecord>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // 庫存不可為負（對應 UPDATE ... WHERE Quantity > 0）
        b.Entity<BookItem>()
            .ToTable(t => t.HasCheckConstraint("CK_Books_Quantity", "[Quantity] >= 0"));

        // 分類下還有書時不可刪除分類（預設會 Cascade，必須改掉）
        b.Entity<BookItem>()
            .HasOne(x => x.Category).WithMany()
            .OnDelete(DeleteBehavior.Restrict);

        // 刪書時連同借閱歷史一併刪除（未歸還的由業務規則先擋下）
        b.Entity<BorrowRecord>()
            .HasOne(x => x.Book).WithMany()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
