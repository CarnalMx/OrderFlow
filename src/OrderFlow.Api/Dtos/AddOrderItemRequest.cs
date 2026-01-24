namespace OrderFlow.Api.Dtos;

public record AddOrderItemRequest(
    string Name,
    int Quantity,
    decimal UnitPrice
);