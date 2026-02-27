using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using MP3Pro.ViewModels;

namespace MP3Pro
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new MainViewModel();
        }

        private void Waveform_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && this.DataContext is MainViewModel vm)
            {
                // Tıklanan noktayı al ve seçimi başlat
                Point p = e.GetPosition(border);
                vm.StartSelection(p.X, border.ActualWidth);

                // Fareyi Border'a kilitle
                border.CaptureMouse();
            }
        }

        private void Waveform_MouseMove(object sender, MouseEventArgs e)
        {
            // Sadece fare Border tarafından kilitlenmişse (yani tıklanıp sürükleniyorsa) işlem yap
            if (sender is Border border && this.DataContext is MainViewModel vm && border.IsMouseCaptured)
            {
                Point p = e.GetPosition(border);
                vm.UpdateSelection(p.X, border.ActualWidth);
            }
        }

        private void Waveform_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                // Fare bırakıldığında kilidi Window'dan DEĞİL, Border'dan kaldır!
                border.ReleaseMouseCapture();
            }
        }
    }
}