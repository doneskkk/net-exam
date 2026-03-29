namespace ExamTest.Models;

public sealed class Participant
{
    public int Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string DisplayLabel => $"{FullName} | {Email}";
}
