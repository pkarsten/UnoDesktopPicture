using Microsoft.UI.Xaml.Data;

namespace PiPic1;
    public class ComboBoxTaskFolderItemConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as MSGraph.Response.TaskFolder;
        }
    }

    public class ComboBoxTaskResponseItemConvert : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as MSGraph.Response.TaskResponse;
        }
    }
