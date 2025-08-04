using System;
using Microsoft.Maui.Controls;
using MinistryTracker.Models;
using MinistryTracker.ViewModels;

namespace MinistryTracker.Views
{
    public partial class EditStudentPage : ContentPage
    {
        public EditStudentPage(Student student)
        {
            InitializeComponent();
            BindingContext = new EditStudentViewModel(student, App.Database);
        }
    }
}
