using System;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Data;
using MinistryTracker.Models;

namespace MinistryTracker.Views
{
    public partial class StudentProfilePage : ContentPage
    {
        // keep your existing constructor; only the handler below matters

        private async void OnEditStudentClicked(object sender, EventArgs e)
        {
            try
            {
                var bc = BindingContext;
                Student? student = null;

                // Try common property names
                foreach (var name in new[] { "Model", "Student", "Current", "SelectedStudent", "Item" })
                {
                    var p = bc?.GetType().GetProperty(name);
                    if (p is not null)
                    {
                        student = p.GetValue(bc) as Student;
                        if (student is not null) break;
                    }
                }

                // Fallback: try StudentId -> fetch
                if (student is null)
                {
                    var idProp = bc?.GetType().GetProperty("StudentId")
                              ?? bc?.GetType().GetProperty("SelectedStudentId");
                    if (idProp is not null && idProp.GetValue(bc) is int id && id > 0)
                    {
                        var sp = Application.Current?.Handler?.MauiContext?.Services;
                        var data = sp?.GetRequiredService<DataService>();
                        if (data is not null) student = await data.GetStudentByIdAsync(id);
                    }
                }

                if (student is null)
                {
                    await DisplayAlert("Oops", "No student loaded.", "OK");
                    return;
                }

                var sp2 = Application.Current?.Handler?.MauiContext?.Services;
                var editPage = sp2?.GetRequiredService<EditStudentPage>();
                if (editPage is null)
                {
                    await DisplayAlert("Error", "Edit page not available.", "OK");
                    return;
                }

                editPage.Init(student);
                await Navigation.PushAsync(editPage);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        // If XAML references this, keep it so XAML compiles:
        private async void OnAddVisitClicked(object sender, EventArgs e)
            => await DisplayAlert("Add Visit", "Add Visit feature coming soon.", "OK");
    }
}
