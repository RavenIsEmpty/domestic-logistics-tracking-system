using LogisticsTracking.Api.Entities;
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

        // POST api/Shipments
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

        // GET api/Shipments/{trackingCode}
        [HttpGet("{trackingCode}")]
        public async Task<ActionResult<ShipmentDetailsResponse>> GetByTrackingCode(string trackingCode)
        {
            try
            {
                var shipment = await _shipmentService.GetByTrackingCodeAsync(trackingCode);

                if (shipment == null)
                {
                    return NotFound(new { message = "Shipment not found." });
                }

                var response = new ShipmentDetailsResponse
                {
                    TrackingCode = shipment.TrackingCode,
                    Status = shipment.Status,
                    SenderName = shipment.SenderName,
                    SenderPhone = shipment.SenderPhone,
                    ReceiverName = shipment.ReceiverName,
                    ReceiverPhone = shipment.ReceiverPhone,
                    OriginBranchName = shipment.OriginBranch.Name,
                    DestinationBranchName = shipment.DestinationBranch.Name,
                    CreatedAt = shipment.CreatedAt,
                    Events = shipment.Events.Select(e => new TrackingEventResponse
                    {
                        Status = e.Status.ToString(),
                        Description = e.Description,
                        CreatedAt = e.CreatedAt,
                        Lat = e.Lat,
                        Lng = e.Lng,
                        LocationText = e.LocationText
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipment by tracking code {TrackingCode}", trackingCode);
                return StatusCode(500, new { message = "An error occurred while retrieving the shipment." });
            }
        }
    }
}
