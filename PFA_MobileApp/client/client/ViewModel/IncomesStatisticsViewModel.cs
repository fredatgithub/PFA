﻿using System.Collections.ObjectModel;
using System.Globalization;
using client.Infrastructure;
using client.Model.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microcharts;
using SkiaSharp;

namespace client.ViewModel;

[QueryProperty(nameof(Incomes), "Incomes")]
public partial class IncomesStatisticsViewModel : BaseViewModel
{
    public IncomesStatisticsViewModel()
    {
        EventManager.OnUserExit += UserExitHandler;
    }
 
    public Task CompleteDataAfterNavigation()
    {
        ChartDateTime = DateTime.Now;
        ChartMonth = ChartDateTime.ToString("Y", CultureInfo.InvariantCulture);
        
        CompleteEntries();
        
        return Task.CompletedTask;
    }

    [ObservableProperty] private DonutChart _chart;
    
    [ObservableProperty] private ObservableCollection<ChartEntry> _entries;
    
    [ObservableProperty] private ObservableCollection<IncomeModel> _incomes;
    
    [ObservableProperty] private ObservableCollection<StatisticModel<decimal>> _incomesStatisticModels;

    [ObservableProperty] private DateTime _chartDateTime;

    [ObservableProperty] private string _chartMonth;

    [RelayCommand]
    private void SwipeRight()
    {
        ChartDateTime = ChartDateTime.AddMonths(-1);
        ChartMonth = ChartDateTime.ToString("Y", CultureInfo.InvariantCulture);
        
        CompleteEntries();
    }
    
    [RelayCommand]
    private void SwipeLeft ()
    {
        ChartDateTime = ChartDateTime.AddMonths(1);
        ChartMonth = ChartDateTime.ToString("Y", CultureInfo.InvariantCulture);
        
        CompleteEntries();
    }

    private void CompleteEntries()
    {
        Entries = [];
        IncomesStatisticModels = [];
        
        var incomesStatisticModelsList = Incomes
            .Where(x => x.Date.Month == ChartDateTime.Month && x.Date.Year == ChartDateTime.Year && x.Value != 0)
            .GroupBy(x => x.IncomeType)
            .Select(x => new StatisticModel<decimal>
            {
                TypeName = x.Key,
                Value = x.Sum(income => income.Value)
            })
            .ToList();

        IncomesStatisticModels = new ObservableCollection<StatisticModel<decimal>>(incomesStatisticModelsList);
        
        for (var iterator = 0; iterator < IncomesStatisticModels.Count; iterator++)
        {
            var colorsIterator = iterator >= ChartHelpers.ChartColors.Count
                ? iterator % ChartHelpers.ChartColors.Count
                : iterator;
            
            var entry = new ChartEntry((float)IncomesStatisticModels[iterator].Value)
            {
                Color = ChartHelpers.ChartColors[colorsIterator],
                Label = IncomesStatisticModels[iterator].TypeName,
                TextColor = ChartHelpers.ChartColors[colorsIterator],
                ValueLabel = IncomesStatisticModels[iterator].Value.ToString(CultureInfo.CurrentCulture)
            };
            
            Entries.Add(entry);
        }

        if (!IncomesStatisticModels.Any())
        {
            IncomesStatisticModels.Add(new StatisticModel<decimal>
            {
                TypeName = "Nothing",
                Value = 0
            });
        }
        
        if (!Entries.Any())
        {
            Entries.Add(new ChartEntry(1)
            {
                Color = ChartHelpers.ChartColors[^1],
                Label = "Nothing",
                TextColor = ChartHelpers.ChartColors[^1],
                ValueLabel = "0"
            });
        }
        
        Chart = new DonutChart
        {
            Entries = Entries,
            LabelTextSize = 40,
            HoleRadius = 0.3f,
            BackgroundColor = SKColor.Empty,
            LabelMode = LabelMode.None
        };
    }
    
    private Task UserExitHandler()
    {
        Incomes = [];
        return Task.CompletedTask;
    }
}