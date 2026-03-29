using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace ExamTest;

internal static partial class ValidationHelper
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    public static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    public static int RequirePositiveInt(string? value, string fieldName)
    {
        if (!int.TryParse(value, out var parsedValue) || parsedValue <= 0)
        {
            throw new InvalidOperationException($"{fieldName} must be a positive number.");
        }

        return parsedValue;
    }

    public static string RequireEmail(string? value)
    {
        var email = RequireText(value, "Email");
        if (!EmailRegex().IsMatch(email))
        {
            throw new InvalidOperationException("Email format is invalid.");
        }

        return email;
    }

    public static DateTime RequireEventDate(DatePicker datePicker, string? timeText)
    {
        if (datePicker.SelectedDate is null)
        {
            throw new InvalidOperationException("Event date is required.");
        }

        if (!TimeSpan.TryParse(RequireText(timeText, "Event time"), out var parsedTime))
        {
            throw new InvalidOperationException("Time must be in HH:mm format.");
        }

        return datePicker.SelectedDate.Value.Date.Add(parsedTime);
    }

    public static string RequireComboBoxText(ComboBox comboBox, string fieldName)
    {
        var selectedValue = comboBox.SelectedItem as string ?? comboBox.Text;
        return RequireText(selectedValue, fieldName);
    }

    public static int RequireSelectedValue(ComboBox comboBox, string fieldName)
    {
        if (comboBox.SelectedValue is int id && id > 0)
        {
            return id;
        }

        throw new InvalidOperationException($"{fieldName} is required.");
    }
}
