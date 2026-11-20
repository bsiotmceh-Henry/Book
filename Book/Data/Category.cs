using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Book.Data;

/// <summary>書籍分類。名稱唯一。</summary>
[Table("Categories")]
[Index(nameof(Name), IsUnique = true)]
public class Category
{
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = "";

    public int Sort { get; set; }
}
