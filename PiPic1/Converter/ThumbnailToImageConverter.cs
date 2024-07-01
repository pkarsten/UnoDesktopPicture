using System;
using Windows.Storage.FileProperties;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace PiPic1;

public class ThumbnailToImageConverter : IValueConverter

{

    public object Convert(object value, Type targetType, object parameter, string language)

    {
        BitmapImage image = null;

        if (value != null)
        {
            if (value.GetType() != typeof(StorageItemThumbnail))
            {
                throw new ArgumentException("Expected a thumbnail");
            }
            if (targetType != typeof(ImageSource))
            {
                throw new ArgumentException("What are you trying to convert to here?");
            }
            StorageItemThumbnail thumbnail = (StorageItemThumbnail)value;
            image = new BitmapImage();
            image.SetSource(thumbnail);

        }
        return (image);

    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)

    {

        throw new NotImplementedException();

    }

}
