using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Brokerage.Api.Models;

namespace Brokerage.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController: ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrdersController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Cria uma ordem
    /// </summary>
    /// <param name="request">Body</param>
    /// <returns>Retorna a ordem criada com status 201 (Created)</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var order = new Order(
            request.CustomerId,
            request.StockSymbol,
            request.Quantity,
            request.Price
        );
        
        await _orderRepository.CreateAsync(order);
        
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    /// <summary>
    /// Busca ordem pelo ID
    /// </summary>
    /// <param name="id">Id da ordem</param>
    /// <returns>Retorna a ordem encontrada ou 404 (Not Found) se não existir</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);

        if (order == null)
        {
            return NotFound();
        }

        return Ok(order);
    }
}