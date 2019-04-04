namespace Hermes.ORM.Tests;

/// <summary>
///     测试用户实体
/// </summary>
public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
///     测试订单实体
/// </summary>
public sealed class Order
{
    public int Id { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

/// <summary>
///     测试商品实体
/// </summary>
public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public double Price { get; set; }
    public string Category { get; set; } = "";
}