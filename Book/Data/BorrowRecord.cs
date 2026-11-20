using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Book.Data;

/// <summary>借閱紀錄。狀態（剩 N 天 / 逾期）不存欄位，由 BorrowStatus 計算。</summary>
[Table("BorrowRecords")]
[Index(nameof(UserId), nameof(ReturnedAt))]
public class BorrowRecord
{
    public int Id { get; set; }

    [ForeignKey(nameof(Book))]
    public int BookId { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = "";

    [Column(TypeName = "datetime2(0)")]
    public DateTime BorrowedAt { get; set; }

    [Column(TypeName = "date")]
    public DateTime DueDate { get; set; }

    [Column(TypeName = "datetime2(0)")]
    public DateTime? ReturnedAt { get; set; }

    /// <summary>導覽屬性（僅供 EF 建立外鍵，業務程式不使用）。</summary>
    public BookItem? Book { get; set; }
}
