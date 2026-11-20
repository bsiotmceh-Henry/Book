using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Book.Data;

/// <summary>書籍（類別名稱用 BookItem，避免與根命名空間 Book 衝突）。</summary>
[Table("Books")]
[Index(nameof(Title))]
public class BookItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = "";

    [Required, MaxLength(100)]
    public string Author { get; set; } = "";

    [Required, MaxLength(100)]
    public string Publisher { get; set; } = "";

    [ForeignKey(nameof(Category))]
    public int CategoryId { get; set; }

    public int Quantity { get; set; }

    /// <summary>導覽屬性（僅供 EF 建立外鍵，業務程式不使用）。</summary>
    public Category? Category { get; set; }

    public BookItem Clone() => (BookItem)MemberwiseClone();
}
