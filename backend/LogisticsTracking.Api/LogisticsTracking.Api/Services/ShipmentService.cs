using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using LogisticsTracking.Api.Data;
using LogisticsTracking.Api.Dtos;
using LogisticsTracking.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LogisticsTracking.Api.Services
{
    public interface IShipmentService
    {
        Task<Shipment> CreateShipmentAsync(CreateShipmentRequest request);
        Task<Shipment?> GetByTrackingCodeAsync(string trackingCode);

        // ✅ existing
        Task<List<Shipment>> GetShipmentsAsync(ShipmentStatus? status = null);
        Task<Shipment?> AddTrackingEventAsync(string trackingCode, AddTrackingEventRequest request);

        // ✅ cancel shipment
        Task<Shipment?> CancelShipmentAsync(string trackingCode, string? reason);

        // ✅ NEW: add GPS-only tracking event (used by /location)
        Task<Shipment?> AddLocationEventAsync(
            string trackingCode,
            double lat,
            double lng,
            string? locationText);
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

        // ✅ list shipments for admin (optionally filter by status)
        public async Task<List<Shipment>> GetShipmentsAsync(ShipmentStatus? status = null)
        {
            var query = _db.Shipments
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(s => s.Status == status.Value);
            }

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .Take(100) // simple limit for now
                .ToListAsync();
        }

        // ✅ add tracking event + update shipment status
        public async Task<Shipment?> AddTrackingEventAsync(string trackingCode, AddTrackingEventRequest request)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                return null;
            }

            var trackingEvent = new TrackingEvent
            {
                ShipmentId = shipment.Id,
                Status = request.Status,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                Lat = request.Lat,
                Lng = request.Lng,
                LocationText = request.LocationText
            };

            shipment.Status = request.Status;
            shipment.Events.Add(trackingEvent);

            await _db.SaveChangesAsync();

            shipment.Events = shipment.Events
                .OrderBy(e => e.CreatedAt)
                .ToList();

            return shipment;
        }

        // ✅ NEW: GPS-only event (does NOT change main shipment status)
        public async Task<Shipment?> AddLocationEventAsync(
            string trackingCode,
            double lat,
            double lng,
            string? locationText)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                return null;
            }

            var ev = new TrackingEvent
            {
                ShipmentId = shipment.Id,
                // keep whatever the current status is (Pending / InTransit / Delivered / Cancelled)
                Status = shipment.Status,
                Description = string.IsNullOrWhiteSpace(locationText)
                    ? "Driver GPS update"
                    : locationText!,
                CreatedAt = DateTime.UtcNow,
                Lat = lat,
                Lng = lng,
                LocationText = locationText
            };

            shipment.Events.Add(ev);

            await _db.SaveChangesAsync();

            shipment.Events = shipment.Events
                .OrderBy(e => e.CreatedAt)
                .ToList();

            _logger.LogInformation(
                "GPS update for {TrackingCode}: {Lat}, {Lng}",
                shipment.TrackingCode,
                lat,
                lng);

            return shipment;
        }

        // ✅ cancel shipment + add tracking event
        public async Task<Shipment?> CancelShipmentAsync(string trackingCode, string? reason)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OriginBranch)
                .Include(s => s.DestinationBranch)
                .Include(s => s.Events)
                .FirstOrDefaultAsync(s => s.TrackingCode == trackingCode);

            if (shipment == null)
            {
                return null;
            }

            // prevent cancelling delivered/already cancelled shipments
            if (shipment.Status == ShipmentStatus.Delivered || shipment.Status == ShipmentStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot cancel a delivered or already cancelled shipment.");
            }

            shipment.Status = ShipmentStatus.Cancelled;

            var ev = new TrackingEvent
            {
                ShipmentId = shipment.Id,
                Status = ShipmentStatus.Cancelled,
                Description = string.IsNullOrWhiteSpace(reason)
                    ? "Shipment cancelled by admin."
                    : reason!,
                CreatedAt = DateTime.UtcNow
            };

            shipment.Events.Add(ev);

            await _db.SaveChangesAsync();

            // keep events sorted
            shipment.Events = shipment.Events
                .OrderBy(e => e.CreatedAt)
                .ToList();

            _logger.LogInformation("Cancelled shipment {TrackingCode}", shipment.TrackingCode);

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
