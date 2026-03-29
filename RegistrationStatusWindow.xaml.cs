using System.Windows;
using ExamTest.Data;
using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest;

public partial class RegistrationStatusWindow : Window
{
    private readonly SchoolRepository _repository = new();
    private readonly RegistrationRecord _registration;

    public RegistrationStatusWindow(RegistrationRecord registration)
    {
        InitializeComponent();
        _registration = registration;
        StatusComboBox.ItemsSource = UiOptions.RegistrationStatuses;
        StatusComboBox.Text = registration.Status;
        RegistrationInfoTextBlock.Text = $"Registration #{registration.Id}\n{registration.ParticipantName} -> {registration.EventTitle}";
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var status = ValidationHelper.RequireComboBoxText(StatusComboBox, "New status");
            await _repository.UpdateRegistrationStatusAsync(_registration.Id, status);
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
