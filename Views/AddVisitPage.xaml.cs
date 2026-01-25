// FileName: AddVisitPage.xaml.cs — Add visit page (cross-platform only) — 2026-01-24

using Microsoft.Maui.Controls;
using System.Threading.Tasks;

namespace MinistryTracker.Views
{
    public partial class AddVisitPage : ContentPage
    {
        private bool _dateRequested;
        private bool _timeRequested;

        public AddVisitPage(ViewModels.AddVisitViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;

            Loaded += (_, __) => TryOpenDateAsync();
            VisitDatePicker.HandlerChanged += (_, __) => TryOpenDateAsync();
            VisitTimePicker.HandlerChanged += (_, __) => { if (_timeRequested) TryOpenTimeAsync(); };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            TryOpenDateAsync();
        }

        private async void TryOpenDateAsync()
        {
            if (_dateRequested) return;
            _dateRequested = true;

            await Task.Delay(200);

            // Best-effort and safe: only attempt if handler exists
            if (VisitDatePicker?.Handler is null)
            {
                _dateRequested = false;
                return;
            }

            VisitDatePicker.Focus();
        }

        private async void TryOpenTimeAsync()
        {
            await Task.Delay(150);

            if (VisitTimePicker?.Handler is null)
                return;

            VisitTimePicker.Focus();
        }

        private void OnVisitDateSelected(object sender, DateChangedEventArgs e)
        {
            if (BindingContext is not ViewModels.AddVisitViewModel vm)
                return;

            vm.VisitDate = e.NewDate.Date;

            VisitTimePicker.IsEnabled = true;
            _timeRequested = true;
            TryOpenTimeAsync();
        }
    }
}
