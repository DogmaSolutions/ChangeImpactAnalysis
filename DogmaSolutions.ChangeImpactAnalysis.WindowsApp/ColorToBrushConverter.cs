using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DogmaSolutions.ChangeImpactAnalysis.WindowsApp;

public class ColorToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var dc = (System.Drawing.Color)value;
        return new SolidColorBrush(new Color { A = dc.A, R = dc.R, G = dc.G, B = dc.B });
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}