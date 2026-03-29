using System.Windows;
using ExamTest.Data;
using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest;

public partial class ParticipantFormWindow : Window
{
    private readonly SchoolRepository _repository = new();
    private readonly Participant? _existingParticipant;

    public ParticipantFormWindow(Participant? existingParticipant = null)
    {
        InitializeComponent();
        _existingParticipant = existingParticipant;
        TitleTextBlock.Text = existingParticipant is null ? "Add Participant" : "Update Participant";
        Title = existingParticipant is null ? "Add Participant" : "Update Participant";

        if (existingParticipant is not null)
        {
            ParticipantNameTextBox.Text = existingParticipant.FullName;
            ParticipantEmailTextBox.Text = existingParticipant.Email;
            ParticipantPhoneTextBox.Text = existingParticipant.Phone;
        }
    }

    private Participant BuildParticipant()
    {
        return new Participant
        {
            Id = _existingParticipant?.Id ?? 0,
            FullName = ValidationHelper.RequireText(ParticipantNameTextBox.Text, "Participant name"),
            Email = ValidationHelper.RequireEmail(ParticipantEmailTextBox.Text),
            Phone = ValidationHelper.RequireText(ParticipantPhoneTextBox.Text, "Phone")
        };
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var participant = BuildParticipant();
            if (_existingParticipant is null)
            {
                await _repository.AddParticipantAsync(participant);
            }
            else
            {
                await _repository.UpdateParticipantAsync(participant);
            }

            DialogResult = true;
        }
        catch (MySqlException exception)
        {
            MessageBox.Show(exception.Message, "MySQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
