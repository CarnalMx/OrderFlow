namespace OrderFlow.Domain.Models;

public class Order
{
	public int Id { get; set; }
	public string CustomerName { get; set; } = "";
	public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public OrderStatus Status { get; set; } = OrderStatus.Draft;
}
