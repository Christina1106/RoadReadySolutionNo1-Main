namespace RoadReady1.Models.DTOs
{
    public class MaintenanceRequestCreateDto
    {
        public int CarId { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
    }

    public class MaintenanceRequestDto
    {
        public int RequestId { get; set; }
        public int CarId { get; set; }
        public int ReportedBy { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
        public DateTime ReportedDate { get; set; }
        public bool IsResolved { get; set; }
    }
}
