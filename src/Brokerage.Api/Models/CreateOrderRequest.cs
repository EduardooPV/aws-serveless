using System.ComponentModel.DataAnnotations;

namespace Brokerage.Api.Models;

/// <summary>
/// Request para criação de ordem
/// </summary>
public class CreateOrderRequest
{
    /// <summary>
    /// ID do cliente
    /// </summary>
    [Required]
    public required string CustomerId { get; set; }

    /// <summary>
    /// Símbolo da ação
    /// </summary>
    [Required]
    public required string StockSymbol { get; set; }

    /// <summary>
    /// Quantidade da ordem
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    /// <summary>
    /// Preço da ação
    /// </summary>
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }
}
