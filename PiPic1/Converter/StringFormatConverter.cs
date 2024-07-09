using Microsoft.UI.Xaml.Data;

namespace PiPic1;

public class StringFormatConverter : IValueConverter
{
    public string? StringFormat { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (!String.IsNullOrEmpty(StringFormat))
            return String.Format(StringFormat, value);

        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
