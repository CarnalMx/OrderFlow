namespace OrderFlow.Domain.Models;

public class OrderItem
{
    public long Id { get; set; }

    public int OrderId { get; set; }

    public string Name { get; set; } = "";

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal => Quantity * UnitPrice;
}