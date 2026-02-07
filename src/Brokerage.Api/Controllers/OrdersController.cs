using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Brokerage.Api.Models;

namespace Brokerage.Api.Controllers;

/// <summary>
/// Controlador de Ordens
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController(IOrderRepository orderRepository, IOrderQueue orderQueue) : ControllerBase
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IOrderQueue _orderQueue = orderQueue;

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

        await _orderRepository.SaveAsync(order);
        await _orderQueue.PublishOrderCreatedAsync(order.Id.ToString());


        return Accepted(new { order.Id });
    }

    /// <summary>
    /// Busca todas as ordens
    /// </summary>
    /// <returns>Retorna todas as ordens encontradas ou 404 (Not Found) se não existirem</returns>
    [HttpGet]
    public async Task<IActionResult> GetAllOrders()
    {
        var orders = await _orderRepository.GetAllOrdersAsync();

        if (orders == null)
        {
            return NotFound();
        }

        return Ok(orders);
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