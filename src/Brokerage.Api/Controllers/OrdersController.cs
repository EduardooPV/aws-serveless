using System.Text.Json;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Brokerage.Api.Models;
using Brokerage.Domain.Entities;
using Brokerage.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Brokerage.Api.Controllers;

/// <summary>
/// Controlador de Ordens
/// </summary>
[ApiController]
[Route("orders")]
public class OrdersController(IOrderRepository orderRepository, IOrderQueue orderQueue, IAmazonStepFunctions sfn) : ControllerBase
{
    private readonly IOrderRepository _orderRepository = orderRepository;
    private readonly IOrderQueue _orderQueue = orderQueue;
    private readonly IAmazonStepFunctions _sfn = sfn;

    /// <summary>
    /// Cria uma ordem
    /// </summary>
    /// <param name="request">Body</param>
    /// <returns>Retorna a ordem criada com status 201 (Created)</returns>
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromServices] IOrderOrchestrator orchestrator)
    {
        var order = new Order(
            request.CustomerId,
            request.StockSymbol,
            request.Quantity,
            request.Price
        );

        await _orderRepository.SaveAsync(order);

        await orchestrator.StartOrderProcessingSagaAsync(order);

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