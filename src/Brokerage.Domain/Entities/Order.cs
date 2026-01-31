using System;

namespace Brokerage.Domain.Entities;

public class Order
{
    public Order(string customerId, string stockSymbol, int quantity, decimal price)
    {
        Id = Guid.NewGuid();
        CustomerId = customerId;
        StockSymbol = stockSymbol;
        Quantity = quantity;
        Price = price;
        CreatedAt = DateTime.UtcNow;
        Status = "Pending";
    }

    public Order(Guid id, string customerId, string stockSymbol, int quantity, decimal price, string status, DateTime createdAt)
    {
        Id = id;
        CustomerId = customerId;
        StockSymbol = stockSymbol;
        Quantity = quantity;
        Price = price;
        CreatedAt = createdAt;
        Status = status;
    }

    public Guid Id { get; private set; }

    public string CustomerId { get; private set; }

    public string StockSymbol { get; private set; }

    public int Quantity { get; private set; }

    public decimal Price { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public string Status { get; set; }

    public decimal TotalAmount => Quantity * Price;
}