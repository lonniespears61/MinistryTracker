using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

#if ANDROID
using AndroidX.AppCompat.Widget;
#endif

namespace MinistryTracker.Views
{
    public partial class AddVisitPage : ContentPage
    {
        private readonly AddVisitViewModel _vm;

        private bool _dateRequested;
        private bool _timeRequested;

        public AddVisitPage(AddVisitViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            BindingContext = _vm;

            // Try when the XAML view is loaded (layout tree ready)
            Loaded += (_, __) => TryOpenDateAsync();

            // Try again as soon as a native handler exists (Android/iOS)
            VisitDatePicker.HandlerChanged += (_, __) => TryOpenDateAsync();
            VisitTimePicker.HandlerChanged += (_, __) => { if (_timeRequested) TryOpenTimeAsync(); };
        }

        // Called by StudentsListPage before navigation to prefill the VM (also sets StudentName in VM)
        public void Load(Student student) => _vm.Load(student);

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Another safety net after page transition animations
            TryOpenDateAsync();
        }

        private async void TryOpenDateAsync()
        {
            if (_dateRequested) return;
            _dateRequested = true;

            // Let the page finish animating + layout settle
            await Task.Delay(120);

            // 1) Cross-platform attempt
            if (VisitDatePicker.Focus())
                return;

            // 2) Android: force the native click to open the picker
#if ANDROID
            var native = VisitDatePicker?.Handler?.PlatformView as AppCompatEditText;
            if (native != null)
            {
                native.PerformClick();
                return;
            }
#endif

            // If neither path worked, allow retry later (e.g., handler not ready yet)
            _dateRequested = false;
        }

        private async void TryOpenTimeAsync()
        {
            // time open only requested after date picked
            await Task.Delay(100);

            if (VisitTimePicker.Focus())
                return;

#if ANDROID
            var native = VisitTimePicker?.Handler?.PlatformView as AppCompatEditText;
            native?.PerformClick();
#endif
        }

        private void OnVisitDateSelected(object sender, DateChangedEventArgs e)
        {
            _vm.VisitDate = e.NewDate.Date;

            VisitTimePicker.IsEnabled = true;

            // Request opening time picker; HandlerChanged will re-try if not ready yet
            _timeRequested = true;
            TryOpenTimeAsync();
        }
    }
}
