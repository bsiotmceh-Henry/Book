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

    public DateTime CreateAt { get; set; }

    public DateTime UpdateAt { get; set; }

    public Category? Category { get; set; }

    public BookItem Clone() => (BookItem)MemberwiseClone();
}
