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

        public class CancelShipmentRequest
        {
            public string? Reason { get; set; }
        }

        // ✅ NEW: driver GPS → /api/shipments/{trackingCode}/location
        [HttpPost("{trackingCode}/location")]
        public async Task<IActionResult> UpdateLocation(
            string trackingCode,
            [FromBody] UpdateLocationRequest request)
        {
            try
            {
                var shipment = await _shipmentService.AddLocationEventAsync(
                    trackingCode,
                    request.Lat,
                    request.Lng,
                    request.LocationText
                );

                if (shipment == null)
                {
                    return NotFound(new { message = "Shipment not found." });
                }

                // Driver page doesn't use the body, only the status code.
                // 204 = success with no content.
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error updating GPS location for shipment {TrackingCode}",
                    trackingCode);

                return StatusCode(500, new { message = "An error occurred while updating shipment location." });
            }
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

        // ✅ Admin list shipments (optional ?status=0/1/2)
        // GET api/Shipments?status=0
        [HttpGet]
        public async Task<ActionResult<List<ShipmentListItemResponse>>> GetShipments([FromQuery] int? status)
        {
            try
            {
                ShipmentStatus? parsedStatus = null;

                if (status.HasValue)
                {
                    if (!Enum.IsDefined(typeof(ShipmentStatus), status.Value))
                    {
                        return BadRequest(new { message = "Invalid status value." });
                    }

                    parsedStatus = (ShipmentStatus)status.Value;
                }

                var shipments = await _shipmentService.GetShipmentsAsync(parsedStatus);

                var response = shipments.Select(s => new ShipmentListItemResponse
                {
                    Id = s.Id,
                    TrackingCode = s.TrackingCode,
                    Status = s.Status,
                    OriginBranchName = s.OriginBranch.Name,
                    DestinationBranchName = s.DestinationBranch.Name,
                    AssignedDriverId = s.AssignedDriverId,
                    CreatedAt = s.CreatedAt
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing shipments");
                return StatusCode(500, new { message = "An error occurred while listing shipments." });
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

                var response = MapToDetailsResponse(shipment);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting shipment by tracking code {TrackingCode}", trackingCode);
                return StatusCode(500, new { message = "An error occurred while retrieving the shipment." });
            }
        }

        // ✅ add tracking event / update status (Driver or Admin)
        // POST api/Shipments/{trackingCode}/events
        [HttpPost("{trackingCode}/events")]
        public async Task<ActionResult<ShipmentDetailsResponse>> AddEvent(
            string trackingCode,
            [FromBody] AddTrackingEventRequest request)
        {
            try
            {
                var shipment = await _shipmentService.AddTrackingEventAsync(trackingCode, request);

                if (shipment == null)
                {
                    return NotFound(new { message = "Shipment not found." });
                }

                var response = MapToDetailsResponse(shipment);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding tracking event for {TrackingCode}", trackingCode);
                return StatusCode(500, new { message = "An error occurred while updating the shipment." });
            }
        }

        // ✅ cancel shipment (Admin)
        // POST api/Shipments/{trackingCode}/cancel
        [HttpPost("{trackingCode}/cancel")]
        public async Task<ActionResult<ShipmentDetailsResponse>> Cancel(
            string trackingCode,
            [FromBody] CancelShipmentRequest? request)
        {
            try
            {
                var shipment = await _shipmentService.CancelShipmentAsync(
                    trackingCode,
                    request?.Reason
                );

                if (shipment == null)
                {
                    return NotFound(new { message = "Shipment not found." });
                }

                var response = MapToDetailsResponse(shipment);
                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                // e.g. cannot cancel delivered / already cancelled
                _logger.LogWarning(ex, "Validation error when cancelling shipment {TrackingCode}", trackingCode);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling shipment {TrackingCode}", trackingCode);
                return StatusCode(500, new { message = "An error occurred while cancelling the shipment." });
            }
        }

        // helper to build detail response
        private static ShipmentDetailsResponse MapToDetailsResponse(Shipment shipment)
        {
            return new ShipmentDetailsResponse
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
        }
    }
}
