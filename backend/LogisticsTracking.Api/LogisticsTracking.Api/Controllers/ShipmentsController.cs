using LogisticsTracking.Api.Dtos;
using LogisticsTracking.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsTracking.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipmentsController : ControllerBase
    {
        private readonly IShipmentService _shipmentService;
        private readonly ILogger<ShipmentsController> _logger;

        public ShipmentsController(IShipmentService shipmentService, ILogger<ShipmentsController> logger)
        {
            _shipmentService = shipmentService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ShipmentResponse>> CreateShipment([FromBody] CreateShipmentRequest request)
        {
            try
            {
                var shipment = await _shipmentService.CreateShipmentAsync(request);

                var response = new ShipmentResponse
                {
                    Id = shipment.Id,
                    TrackingCode = shipment.TrackingCode,
                    Status = shipment.Status,
                    SenderName = shipment.SenderName,
                    ReceiverName = shipment.ReceiverName,
                    CreatedAt = shipment.CreatedAt
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Validation error when creating shipment");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating shipment");
                return StatusCode(500, new { message = "An error occurred while creating the shipment." });
            }
        }
    }
}
