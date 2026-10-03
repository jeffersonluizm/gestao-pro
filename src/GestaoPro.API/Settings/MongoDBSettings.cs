namespace GestaoPro.API.Settings;

public class MongoDBSettings
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string AnnouncementsCollection { get; set; } = "announcements";
    public string ReservationsCollection { get; set; } = "reservations";
    public string CommonAreasCollection { get; set; } = "commonareas";
    public string VisitorsCollection { get; set; } = "visitors";
    public string ParkingCollection { get; set; } = "parking";
    public string PackagesCollection { get; set; } = "packages";
}
