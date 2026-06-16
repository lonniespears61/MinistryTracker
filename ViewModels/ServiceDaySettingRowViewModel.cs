using CommunityToolkit.Mvvm.ComponentModel;
using MinistryTracker.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MinistryTracker.ViewModels;

public partial class ServiceDaySettingRowViewModel : ObservableObject
{
    [ObservableProperty]
    private DayOfWeek day;

    [ObservableProperty]
    private ServicePeriod period;

    public ServiceDaySettingRowViewModel(DayOfWeek day, ServicePeriod period)
    {
        Day = day;
        Period = period == ServicePeriod.None ? ServicePeriod.Morning : period;
    }

    public IReadOnlyList<DayOfWeek> DayValues { get; } =
    [
        DayOfWeek.Saturday,
        DayOfWeek.Sunday,
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday
    ];

    public IReadOnlyList<ServicePeriod> PeriodValues { get; } =
        Enum.GetValues<ServicePeriod>()
            .Where(x => x != ServicePeriod.None)
            .ToList();
}
