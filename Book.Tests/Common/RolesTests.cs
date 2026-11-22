using Book.Common;

namespace Book.Tests.Common;

/// <summary>Roles.Label：角色代碼轉畫面文字。</summary>
public class RolesTests
{
    [Theory]
    [InlineData(Roles.Admin, "管理員")]
    [InlineData(Roles.User, "一般使用者")]
    [InlineData("Unknown", "一般使用者")]   // 未知角色一律當一般使用者顯示
    [InlineData("", "一般使用者")]
    public void Label_角色轉顯示文字(string role, string expected)
        => Assert.Equal(expected, Roles.Label(role));
}
