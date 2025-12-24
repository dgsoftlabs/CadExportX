using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace ModelSpace
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    ///
    public partial class ACadView : UserControl
    {
        private App mod { get; set; }

        public ACadView(App obj)
        {
            InitializeComponent();

            this.mod = obj;

            // Start spinner animation when loaded
            this.Loaded += ACadView_Loaded;
        }

        private void ACadView_Loaded(object sender, RoutedEventArgs e)
        {
            // Start the spinner animation
            var spinnerStoryboard = this.Resources["SpinnerAnimation"] as Storyboard;
            spinnerStoryboard?.Begin();

            // Start the pulse animation
            var pulseStoryboard = this.Resources["PulseAnimation"] as Storyboard;
            pulseStoryboard?.Begin();
        }
    }

    public class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}