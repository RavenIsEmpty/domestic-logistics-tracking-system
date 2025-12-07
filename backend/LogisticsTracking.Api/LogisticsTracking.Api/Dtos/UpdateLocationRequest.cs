namespace LogisticsTracking.Api.Dtos
{
    public class UpdateLocationRequest
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string? LocationText { get; set; }
    }
}
