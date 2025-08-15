using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using MinistryTracker.Data;
using MinistryTracker.ViewModels;
using MinistryTracker.Views;
using MinistryTracker.Models;
using System;

namespace MinistryTracker;

public static class MauiProgram
{
    // ✅ Make DI container accessible app-wide
    public static IServiceProvider Services { get; private set; } = default!;

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(f =>
            {
                f.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                f.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Services
        builder.Services.AddSingleton<DataService>();

        // ViewModels
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<StudentsListViewModel>();
        builder.Services.AddTransient<AddStudentViewModel>();
        builder.Services.AddTransient<EditStudentViewModel>();
        builder.Services.AddTransient<AddVisitViewModel>();
        builder.Services.AddTransient<StudentProfileViewModel>();

        // Pages
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<StudentsListPage>();
        builder.Services.AddTransient<AddStudentPage>();
        builder.Services.AddTransient<EditStudentPage>();
        builder.Services.AddTransient<AddVisitPage>();
        builder.Services.AddTransient<StudentProfilePage>();

        // -------- FACTORIES (each returns the concrete Page type) --------
        builder.Services.AddTransient<Func<Student, EditStudentPage>>(sp => student =>
        {
            var page = sp.GetRequiredService<EditStudentPage>();
            var vm = sp.GetRequiredService<EditStudentViewModel>();
            vm.Load(student);
            page.BindingContext = vm;
            return page; // ✅ return EditStudentPage
        });

        builder.Services.AddTransient<Func<Student, AddVisitPage>>(sp => student =>
        {
            var page = sp.GetRequiredService<AddVisitPage>();
            var vm = sp.GetRequiredService<AddVisitViewModel>();
            vm.Load(student);
            page.BindingContext = vm;
            return page; // ✅ return AddVisitPage
        });

        builder.Services.AddTransient<Func<Student, StudentProfilePage>>(sp => student =>
        {
            var page = sp.GetRequiredService<StudentProfilePage>();
            var vm = sp.GetRequiredService<StudentProfileViewModel>();
            vm.Load(student);
            page.BindingContext = vm;
            return page; // ✅ return StudentProfilePage
        });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ✅ BUILD FIRST
        var app = builder.Build();

        // ✅ expose DI container globally
        Services = app.Services;

        // ✅ initialize DB once at startup (sync wait is fine here)
        var dataService = Services.GetRequiredService<DataService>();
        dataService.InitializeAsync().GetAwaiter().GetResult();

        // ✅ return the built app
        return app;
    }
}
