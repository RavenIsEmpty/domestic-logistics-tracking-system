namespace LogisticsTracking.Api.Entities
{
    public enum UserRole
    {
        Admin,
        Cashier,
        Driver
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public UserRole Role { get; set; }
    }

    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Address { get; set; }

        public ICollection<Shipment> OriginShipments { get; set; } = new List<Shipment>();
        public ICollection<Shipment> DestinationShipments { get; set; } = new List<Shipment>();
    }

    public enum ShipmentStatus
    {
        Pending,
        InTransit,
        Delivered,
        Cancelled,
        Returned
    }

    public class Shipment
    {
        public int Id { get; set; }
        public string TrackingCode { get; set; } = default!;

        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;

        public int OriginBranchId { get; set; }
        public Branch OriginBranch { get; set; } = default!;

        public int DestinationBranchId { get; set; }
        public Branch DestinationBranch { get; set; } = default!;

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;
        public int? AssignedDriverId { get; set; }
        public User? AssignedDriver { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TrackingEvent> Events { get; set; } = new List<TrackingEvent>();
    }

    public class TrackingEvent
    {
        public int Id { get; set; }

        public int ShipmentId { get; set; }
        public Shipment Shipment { get; set; } = default!;

        public ShipmentStatus Status { get; set; }
        public string Description { get; set; } = default!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double? Lat { get; set; }
        public double? Lng { get; set; }
        public string? LocationText { get; set; }
    }
}
