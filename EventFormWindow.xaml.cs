using System.Windows;
using ExamTest.Data;
using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest;

public partial class EventFormWindow : Window
{
    private readonly SchoolRepository _repository = new();
    private readonly EventItem? _existingEvent;

    public EventFormWindow(EventItem? existingEvent = null)
    {
        InitializeComponent();
        _existingEvent = existingEvent;
        EventTypeComboBox.ItemsSource = UiOptions.EventTypes;
        TitleTextBlock.Text = existingEvent is null ? "Add Event" : "Update Event";
        Title = existingEvent is null ? "Add Event" : "Update Event";

        if (existingEvent is not null)
        {
            EventTitleTextBox.Text = existingEvent.Title;
            EventTypeComboBox.Text = existingEvent.EventType;
            EventLocationTextBox.Text = existingEvent.Location;
            EventDatePicker.SelectedDate = existingEvent.EventDate.Date;
            EventTimeTextBox.Text = existingEvent.EventDate.ToString("HH:mm");
            EventCapacityTextBox.Text = existingEvent.Capacity.ToString();
        }
    }

    private EventItem BuildEvent()
    {
        return new EventItem
        {
            Id = _existingEvent?.Id ?? 0,
            Title = ValidationHelper.RequireText(EventTitleTextBox.Text, "Event title"),
            EventType = ValidationHelper.RequireComboBoxText(EventTypeComboBox, "Event type"),
            Location = ValidationHelper.RequireText(EventLocationTextBox.Text, "Location"),
            EventDate = ValidationHelper.RequireEventDate(EventDatePicker, EventTimeTextBox.Text),
            Capacity = ValidationHelper.RequirePositiveInt(EventCapacityTextBox.Text, "Capacity")
        };
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var eventItem = BuildEvent();
            if (_existingEvent is null)
            {
                await _repository.AddEventAsync(eventItem);
            }
            else
            {
                await _repository.UpdateEventAsync(eventItem);
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
