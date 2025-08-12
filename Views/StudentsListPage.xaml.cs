using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    /// <summary>
    /// Code-behind for StudentsListPage.xaml.
    /// Handles UI events and page lifecycle.
    /// This page uses constructor injection for:
    ///     - StudentsListViewModel (_vm): The page's BindingContext.
    ///     - IServiceProvider (_services): For resolving navigation targets.
    /// </summary>
    public partial class StudentsListPage : ContentPage
    {
        // Backing field for the page's ViewModel (injected)
        private readonly StudentsListViewModel _vm;

        // Service provider for resolving other pages (AddStudentPage, EditStudentPage)
        private readonly IServiceProvider _services;

        /// <summary>
        /// Constructor is called by the DI container when navigating to this page.
        /// </summary>
        /// <param name="vm">The injected StudentsListViewModel instance.</param>
        /// <param name="services">The application's service provider (DI container).</param>
        public StudentsListPage(StudentsListViewModel vm, IServiceProvider services)
        {
            InitializeComponent();

            // Assign the injected ViewModel to the page's BindingContext
            BindingContext = _vm = vm;

            // Keep a reference to the DI service provider for resolving other pages
            _services = services;
        }

        /// <summary>
        /// OnAppearing is called every time the page becomes visible.
        /// We use it to load or refresh the Students list.
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Load student data from the database (async)
            await _vm.LoadAsync();

            // Ensure any previous selection is cleared
            StudentsCollection.SelectedItem = null;
        }

        /// <summary>
        /// Event handler for the floating Add button.
        /// Navigates to AddStudentPage via DI container.
        /// </summary>
        private async void OnAddStudentClicked(object sender, EventArgs e)
        {
            // Resolve AddStudentPage instance from DI container
            var addPage = _services.GetRequiredService<AddStudentPage>();

            // Navigate to AddStudentPage
            await Navigation.PushAsync(addPage);
        }

        /// <summary>
        /// Event handler when a student is selected from the list.
        /// Navigates to EditStudentPage with the selected student's data.
        /// </summary>
        private async void OnStudentSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Extract the first selected item (if any)
            if (e.CurrentSelection?.FirstOrDefault() is StudentViewModel svm)
            {
                // Resolve EditStudentPage instance from DI
                var editPage = _services.GetRequiredService<EditStudentPage>();

                // Pass the selected student's model to the Edit page's ViewModel
                editPage.Init(svm.Model);

                // Navigate to EditStudentPage
                await Navigation.PushAsync(editPage);

                // Clear selection so the same student can be selected again later
                StudentsCollection.SelectedItem = null;
            }
        }

        /// <summary>
        /// Event handler for the SearchBar's TextChanged event.
        /// Optional: you can move filtering logic into the ViewModel
        /// for a cleaner MVVM approach.
        /// </summary>
        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Get trimmed search query
            var query = e.NewTextValue?.Trim() ?? string.Empty;

            // TODO: Option 1 - Bind SearchBar.Text to a ViewModel property
            // and filter Students inside the VM.
            //
            // Option 2 - Filter here in code-behind:
            // var filtered = _vm.Students
            //     .Where(s => s.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            //     .ToList();
            //
            // Then assign filtered list to a display collection.
        }
    }
}
