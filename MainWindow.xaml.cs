using System.Windows;
using ExamTest.Data;
using ExamTest.Models;
using MySql.Data.MySqlClient;

namespace ExamTest;

public partial class MainWindow : Window
{
    private readonly SchoolRepository _repository = new();

    private List<EventItem> _allEvents = [];
    private List<EventItem> _events = [];
    private List<Participant> _allParticipants = [];
    private List<Participant> _participants = [];
    private List<RegistrationRecord> _registrations = [];

    public MainWindow()
    {
        InitializeComponent();
        ConfigureStaticControls();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeApplicationAsync();
    }

    private void ConfigureStaticControls()
    {
        EventTypeFilterComboBox.ItemsSource = CreateFilterOptions(UiOptions.EventTypes);
        EventTypeFilterComboBox.SelectedIndex = 0;

        RegistrationFilterStatusComboBox.ItemsSource = CreateFilterOptions(UiOptions.RegistrationStatuses);
        RegistrationFilterStatusComboBox.SelectedIndex = 0;
    }

    private static string[] CreateFilterOptions(IEnumerable<string> values)
    {
        return (new[] { "All" }).Concat(values).ToArray();
    }

    private async Task InitializeApplicationAsync()
    {
        try
        {
            SetStatus("Initializing database, procedures, and view...");
            await _repository.InitializeDatabaseAsync();
            await LoadAllDataAsync();
            SetStatus("Connected to MySQL. Separate create/update forms are ready.");
        }
        catch (Exception exception)
        {
            SetStatus("Connection failed. Check that MySQL is running and root has no password.");
            MessageBox.Show(
                $"Could not connect to MySQL.\n\n{exception.Message}\n\nExpected connection:\n{AppDb.ConnectionString}",
                "Database Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async Task LoadAllDataAsync()
    {
        await LoadEventsAsync();
        await LoadParticipantsAsync();
        await LoadRegistrationsAsync();
    }

    private async Task LoadEventsAsync()
    {
        var typeFilter = EventTypeFilterComboBox.SelectedItem as string;
        if (typeFilter == "All")
        {
            typeFilter = null;
        }

        var locationFilter = NormalizeFilter(EventLocationFilterTextBox.Text);
        _allEvents = await _repository.GetEventsAsync();
        _events = await _repository.GetEventsAsync(typeFilter, locationFilter);
        EventsDataGrid.ItemsSource = _events;
    }

    private async Task LoadParticipantsAsync()
    {
        _allParticipants = await _repository.GetParticipantsAsync();
        _participants = await _repository.GetParticipantsAsync(NormalizeFilter(ParticipantSearchTextBox.Text));
        ParticipantsDataGrid.ItemsSource = _participants;
    }

    private async Task LoadRegistrationsAsync()
    {
        var statusFilter = RegistrationFilterStatusComboBox.SelectedItem as string;
        if (statusFilter == "All")
        {
            statusFilter = null;
        }

        _registrations = await _repository.GetRegistrationsAsync(
            NormalizeFilter(RegistrationSearchTextBox.Text),
            statusFilter);

        RegistrationsDataGrid.ItemsSource = _registrations;
    }

    private static string? NormalizeFilter(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void SetStatus(string message)
    {
        StatusTextBlock.Text = $"Status: {message}";
    }

    private async Task ExecuteCrudAsync(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            await LoadAllDataAsync();
            SetStatus(successMessage);
        }
        catch (MySqlException exception)
        {
            MessageBox.Show(exception.Message, "MySQL Error", MessageBoxButton.OK, MessageBoxImage.Error);
            SetStatus("MySQL operation failed.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            SetStatus("Validation failed.");
        }
    }

    private async Task OpenDialogAndRefreshAsync(Window dialog, string successMessage)
    {
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            await LoadAllDataAsync();
            SetStatus(successMessage);
        }
    }

    private static T RequireSelectedItem<T>(object? selectedItem, string entityName) where T : class
    {
        return selectedItem as T ?? throw new InvalidOperationException($"Select a {entityName} first.");
    }

    private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(LoadAllDataAsync, "All tables refreshed and synchronized.");
    }

    private async void AddEventButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenDialogAndRefreshAsync(new EventFormWindow(), "Event added.");
    }

    private async void UpdateEventButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var eventItem = RequireSelectedItem<EventItem>(EventsDataGrid.SelectedItem, "event");
            await OpenDialogAndRefreshAsync(new EventFormWindow(eventItem), "Event updated.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteEventButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(async () =>
        {
            var eventItem = RequireSelectedItem<EventItem>(EventsDataGrid.SelectedItem, "event");
            await _repository.DeleteEventAsync(eventItem.Id);
        }, "Event deleted.");
    }

    private async void ApplyEventFilterButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(LoadEventsAsync, "Event filters applied.");
    }

    private async void ResetEventFilterButton_Click(object sender, RoutedEventArgs e)
    {
        EventTypeFilterComboBox.SelectedIndex = 0;
        EventLocationFilterTextBox.Clear();
        await ExecuteCrudAsync(LoadEventsAsync, "Event filters reset.");
    }

    private async void AddParticipantButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenDialogAndRefreshAsync(new ParticipantFormWindow(), "Participant added via stored procedure.");
    }

    private async void UpdateParticipantButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var participant = RequireSelectedItem<Participant>(ParticipantsDataGrid.SelectedItem, "participant");
            await OpenDialogAndRefreshAsync(new ParticipantFormWindow(participant), "Participant updated.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void DeleteParticipantButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(async () =>
        {
            var participant = RequireSelectedItem<Participant>(ParticipantsDataGrid.SelectedItem, "participant");
            await _repository.DeleteParticipantAsync(participant.Id);
        }, "Participant deleted.");
    }

    private async void SearchParticipantsButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(LoadParticipantsAsync, "Participant search applied.");
    }

    private async void ResetParticipantsButton_Click(object sender, RoutedEventArgs e)
    {
        ParticipantSearchTextBox.Clear();
        await ExecuteCrudAsync(LoadParticipantsAsync, "Participant search reset.");
    }

    private async void CreateRegistrationButton_Click(object sender, RoutedEventArgs e)
    {
        await OpenDialogAndRefreshAsync(
            new RegistrationCreateWindow(_allEvents, _allParticipants),
            "Registration created through stored procedure and transaction.");
    }

    private async void UpdateRegistrationStatusButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var registration = RequireSelectedItem<RegistrationRecord>(RegistrationsDataGrid.SelectedItem, "registration");
            await OpenDialogAndRefreshAsync(
                new RegistrationStatusWindow(registration),
                "Registration status updated through stored procedure.");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SearchRegistrationsButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteCrudAsync(LoadRegistrationsAsync, "Registration filters applied.");
    }

    private async void ResetRegistrationsButton_Click(object sender, RoutedEventArgs e)
    {
        RegistrationSearchTextBox.Clear();
        RegistrationFilterStatusComboBox.SelectedIndex = 0;
        await ExecuteCrudAsync(LoadRegistrationsAsync, "Registration filters reset.");
    }
}
