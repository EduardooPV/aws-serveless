using System.ComponentModel.DataAnnotations;

namespace Brokerage.Api.Models;

public class CreateOrderRequest
{
    [Required]
    public string CustomerId { get; set; }

    [Required]
    public string StockSymbol { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}