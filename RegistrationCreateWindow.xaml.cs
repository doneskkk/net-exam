using System.Windows;
using ExamTest.Data;
using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest;

public partial class RegistrationCreateWindow : Window
{
    private readonly SchoolRepository _repository = new();

    public RegistrationCreateWindow(IEnumerable<EventItem> events, IEnumerable<Participant> participants)
    {
        InitializeComponent();
        EventComboBox.ItemsSource = events.ToList();
        ParticipantComboBox.ItemsSource = participants.ToList();
        StatusComboBox.ItemsSource = UiOptions.RegistrationStatuses;
        StatusComboBox.SelectedIndex = 0;
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var eventId = ValidationHelper.RequireSelectedValue(EventComboBox, "Event");
            var participantId = ValidationHelper.RequireSelectedValue(ParticipantComboBox, "Participant");
            var status = ValidationHelper.RequireComboBoxText(StatusComboBox, "Registration status");

            await _repository.CreateRegistrationAsync(eventId, participantId, status);
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
