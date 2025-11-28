using System;
using System.Linq;
using System.Threading.Tasks;
using LogisticsTracking.Api.Data;
using LogisticsTracking.Api.Dtos;
using LogisticsTracking.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsTracking.Api.Services
{
    public interface IShipmentService
    {
        Task<Shipment> CreateShipmentAsync(CreateShipmentRequest request);
        Task<Shipment?> GetByTrackingCodeAsync(string trackingCode);
    }

    public class ShipmentService : IShipmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ShipmentService> _logger;

        public ShipmentService(ApplicationDbContext db, ILogger<ShipmentService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<Shipment> CreateShipmentAsync(CreateShipmentRequest request)
        {
            // (simple validation – you can improve later)
            var originExists = await _db.Branches.AnyAsync(b => b.Id == request.OriginBranchId);
            var destExists = await _db.Branches.AnyAsync(b => b.Id == request.DestinationBranchId);

            if (!originExists || !destExists)
            {
                throw new InvalidOperationException("Origin or destination branch does not exist.");
            }

            var shipment = new Shipment
            {
                SenderName = request.SenderName,
                SenderPhone = request.SenderPhone,
                ReceiverName = request.ReceiverName,
                ReceiverPhone = request.ReceiverPhone,
                OriginBranchId = request.OriginBranchId,
                DestinationBranchId = request.DestinationBranchId,
                AssignedDriverId = request.AssignedDriverId,
                Status = ShipmentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
            };

            shipment.TrackingCode = GenerateTrackingCode();

            // initial tracking event
            var initialEvent = new TrackingEvent
            {
                Shipment = shipment,
                Status = shipment.Status,
                Description = "Shipment created",
                CreatedAt = DateTime.UtcNow
            };

            _db.Shipments.Add(shipment);
            _db.TrackingEvents.Add(initialEvent);

            await _db.SaveChangesAsync();

            _logger.LogInformation("Created shipment {TrackingCode} with Id {Id}", shipment.TrackingCode, shipment.Id);

            return shipment;
        }

        public async Task<Shipment?> GetByTrackingCodeAsync(string trackingCode)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
                return null;

            // sort events by time
            shipment.Events = shipment.Events
                .OrderBy(e => e.CreatedAt)
                .ToList();

            return shipment;
        }

        private string GenerateTrackingCode()
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(); // 6 chars
            return $"KH-{datePart}-{randomPart}";
        }
    }
}
