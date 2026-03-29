namespace ExamTest.Models;

public sealed class EventItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public DateTime EventDate { get; set; }

    public int Capacity { get; set; }

    public int AvailableSeats { get; set; }

    public string DisplayLabel => $"{Title} | {EventType} | {Location} | free: {AvailableSeats}";
}
