using LogisticsTracking.Api.Entities;

namespace LogisticsTracking.Api.Dtos
{
    public class CreateShipmentRequest
    {
        public string SenderName { get; set; } = default!;
        public string SenderPhone { get; set; } = default!;
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;

        public int OriginBranchId { get; set; }
        public int DestinationBranchId { get; set; }

        public int? AssignedDriverId { get; set; }  // null = not assigned yet
    }

    public class ShipmentResponse
    {
        public int Id { get; set; }
        public string TrackingCode { get; set; } = default!;
        public ShipmentStatus Status { get; set; }
        public string SenderName { get; set; } = default!;
        public string ReceiverName { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }
}
