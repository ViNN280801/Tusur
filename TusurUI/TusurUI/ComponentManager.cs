using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace TusurUI
{
    class ComponentManager
    {
        public ComponentManager() { }

        public static void ChangeIndicatorPicture(Image indicator, string imagePath)
        {
            if (indicator == null)
                throw new ArgumentNullException(nameof(indicator));
            if (string.IsNullOrWhiteSpace(imagePath))
                throw new ArgumentException("imagePath cannot be null or empty.", nameof(imagePath));

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(imagePath, UriKind.Relative);
            image.EndInit();

            indicator.Source = image;
        }
    }
}
