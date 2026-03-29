namespace ExamTest.Models;

public sealed class RegistrationRecord
{
    public int Id { get; set; }

    public int EventId { get; set; }

    public int ParticipantId { get; set; }

    public string EventTitle { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string Location { get; set; } = string.Empty;

    public string ParticipantName { get; set; } = string.Empty;

    public string ParticipantEmail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime RegisteredAt { get; set; }
}
