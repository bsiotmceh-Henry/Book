using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Book.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategoriesAndBooks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name", "Sort" },
                values: new object[,]
                {
                    { 1, "程式設計", 1 },
                    { 2, "資料庫", 2 },
                    { 3, "軟體工程", 3 },
                    { 4, "其他", 4 }
                });

            migrationBuilder.InsertData(
                table: "Books",
                columns: new[] { "Id", "Author", "CategoryId", "CreateAt", "Publisher", "Quantity", "Title", "UpdateAt" },
                values: new object[,]
                {
                    { 1, "Robert Martin", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prentice Hall", 3, "Clean Code", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "Silberschatz", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "McGraw-Hill", 1, "資料庫系統概念", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "GoF", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 0, "設計模式", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Andrew Hunt", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 2, "The Pragmatic Programmer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Martin Fowler", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 2, "重構", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "Steve McConnell", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Microsoft Press", 1, "Code Complete", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Frederick Brooks", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 4, "人月神話", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Alan Beaulieu", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 3, "SQL 學習指南", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "Baron Schwartz", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 0, "高效能 MySQL", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "Andrew Stellman", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 5, "深入淺出 C#", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "Jon Skeet", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 2, "C# in Depth", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "Andrew Lock", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 3, "ASP.NET Core in Action", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "Jon P Smith", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 2, "Entity Framework Core in Action", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "Eric Evans", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 1, "領域驅動設計", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "Thomas Cormen", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "MIT Press", 2, "演算法導論", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "Eric Freeman", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 3, "Head First 設計模式", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "Robert Martin", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prentice Hall", 2, "敏捷軟體開發", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "Roy Osherove", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 1, "單元測試的藝術", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "Martin Kleppmann", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 2, "資料密集型應用系統設計", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "Itzik Ben-Gan", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Microsoft Press", 1, "SQL Server 效能調校", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "Robert Martin", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prentice Hall", 3, "程式設計師的職業素養", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "Paul Graham", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 2, "黑客與畫家", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "James Clear", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Avery", 4, "原子習慣", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "Robert Martin", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Prentice Hall", 2, "clean Architecture", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 25, "Chris Sainty", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 2, "Blazor in Action", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 26, "David Flanagan", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 3, "JavaScript 權威指南", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 27, "Kyle Simpson", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 1, "你不知道的 JS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 28, "Eric Matthes", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No Starch", 6, "Python 程式設計入門", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 29, "William Shotts", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "No Starch", 2, "Linux 指令大全", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 30, "Behrouz Forouzan", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "McGraw-Hill", 3, "計算機概論", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 31, "Silberschatz", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Wiley", 2, "作業系統概念", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 32, "Andrew Tanenbaum", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pearson", 1, "電腦網路", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 33, "Alfred Aho", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pearson", 0, "編譯器設計", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 34, "Mark Weiss", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Pearson", 3, "資料結構與演算法", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 35, "Mark Richards", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 2, "軟體架構學習指南", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 36, "Sam Newman", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 2, "微服務架構設計", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 37, "Sean Kane", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "O'Reilly", 4, "Docker 技術入門", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 38, "Marko Luksa", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 1, "Kubernetes 實戰", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 39, "Scott Chacon", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Apress", 5, "Git 版本控制", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 40, "Bill Wagner", 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 2, "Effective C#", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 41, "Martin Fowler", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 2, "NoSQL 精粹", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 42, "Josiah Carlson", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Manning", 1, "Redis 實戰", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 43, "Michael Hernandez", 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Addison-Wesley", 3, "資料庫設計入門", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 44, "Don Norman", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Basic Books", 2, "UX 設計心理學", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 45, "Eric Ries", 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Crown", 3, "精實創業", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 46, "Jeff Sutherland", 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Crown", 2, "Scrum 敏捷專案管理", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Books",
                keyColumn: "Id",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
