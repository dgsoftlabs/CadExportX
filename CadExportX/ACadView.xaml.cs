using System.Windows;
using System.Windows.Controls;

namespace ModelSpace
{
    /// <summary>
    /// Interaction logic for Manager.xaml
    /// </summary>
    ///

    public partial class ACadView : UserControl
    {
        private ACadModel mod { get; set; }

        public ACadView(ACadModel obj)
        {
            InitializeComponent();

            this.mod = obj;
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
}