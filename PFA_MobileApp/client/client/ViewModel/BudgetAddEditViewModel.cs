﻿using ApiClient;
using client.Model.Models;
using client.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Interfaces;
using System.Collections.ObjectModel;

namespace client.ViewModel
{
    [QueryProperty(nameof(Budget), "Budget")]
    [QueryProperty(nameof(Budgets), "Budgets")]
    [QueryProperty(nameof(ExpenseTypes), "ExpenseTypes")]
    [QueryProperty(nameof(IncomeTypes), "IncomeTypes")]
    [QueryProperty(nameof(TimePeriods), "TimePeriods")]
    [QueryProperty(nameof(IsEdited), "IsEdited")]

    public partial class BudgetAddEditViewModel : BaseViewModel
    {
        private readonly IPopupNavigation _popupNavigation;
        private readonly Client _client;

        public BudgetAddEditViewModel(IPopupNavigation popupNavigation, Client client)
        {
            _popupNavigation = popupNavigation;
            _client = client;
        }

        public void CompleteDataAfterNavigation()
        {
            if (IsEdited)
            {
                PageTitle = "Edit Budget";

                Name = Budget.Name;
                StartDate = Budget.StartDate;
                TimePeriod = TimePeriods.Where(x => x.Id == Budget.TimePeriodId).FirstOrDefault();

                return;
            }

            PageTitle = "Add Budget";

            StartDate = DateTime.Today;
            TimePeriod = TimePeriods[0];
        }

        [ObservableProperty]
        BudgetModel _budget;

        [ObservableProperty]
        string _pageTitle;

        [ObservableProperty]
        string _name;

        [ObservableProperty]
        DateTime _startDate;

        [ObservableProperty]
        TimePeriodModel _timePeriod;

        [ObservableProperty]
        List<TimePeriodModel> _timePeriods;  

        [ObservableProperty]
        List<ExpenseTypeModel> _expenseTypes;

        [ObservableProperty]
        List<IncomeTypeModel> _incomeTypes;

        [ObservableProperty]
        ObservableCollection<BudgetModel> _budgets;

        public bool IsEdited { get; set; }

        [RelayCommand]
        async Task Save()
        {
            Budget.Name = Name;
            Budget.StartDate = StartDate;
            Budget.TimePeriodId = TimePeriod.Id;
            Budget.TimePeriod = TimePeriod.Name;

            BudgetRequestModel postResult = new();

            try
            {
                var plannedExpensesRequest = new List<PlannedExpensesDTO>();
                var plannedIncomesRequest = new List<PlannedIncomesDTO>();

                if (IsEdited)
                {
                    foreach (var plannedExpenses in Budget.PlannedExpenses)
                    {
                        plannedExpensesRequest.Add(new PlannedExpensesDTO
                        {
                            Id = plannedExpenses.Id,
                            Sum = (double?)plannedExpenses.Sum,
                            BudgetId = plannedExpenses.BudgetId,
                            ExpenseTypeId = plannedExpenses.ExpenseTypeId
                        });
                    }

                    foreach (var plannedIncomes in Budget.PlannedIncomes)
                    {
                        plannedIncomesRequest.Add(new PlannedIncomesDTO
                        {
                            Id = plannedIncomes.Id,
                            Sum = (double?)plannedIncomes.Sum,
                            BudgetId = plannedIncomes.BudgetId,
                            IncomeTypeId = plannedIncomes.IncomeTypeId
                        });
                    }

                    var budgetRequest = new BudgetRequestModel()
                    {
                        Budget = new BudgetDTO()
                        {
                            Id = Budget.Id,
                            Name = Budget.Name,
                            StartDate = Budget.StartDate,
                            TimePeriodId = Budget.TimePeriodId,
                            UserId = Budget.UserId,
                        },
                        PlannedExpenses = plannedExpensesRequest,
                        PlannedIncomes = plannedIncomesRequest
                    };

                    await _client.BudgetPUTAsync(Budget.Id, budgetRequest);
                }
                else
                {
                    foreach (var plannedExpenses in Budget.PlannedExpenses)
                    {
                        plannedExpensesRequest.Add(new PlannedExpensesDTO
                        {
                            Sum = (double?)plannedExpenses.Sum,
                            BudgetId = plannedExpenses.BudgetId,
                            ExpenseTypeId = plannedExpenses.ExpenseTypeId
                        });
                    }

                    foreach (var plannedIncomes in Budget.PlannedIncomes)
                    {
                        plannedIncomesRequest.Add(new PlannedIncomesDTO
                        {
                            Sum = (double?)plannedIncomes.Sum,
                            BudgetId = plannedIncomes.BudgetId,
                            IncomeTypeId = plannedIncomes.IncomeTypeId
                        });
                    }

                    postResult = await _client.BudgetPOSTAsync(
                        new BudgetRequestModel()
                        {
                            Budget = new BudgetDTO()
                            {
                                Name = Budget.Name,
                                StartDate = Budget.StartDate,
                                TimePeriodId = Budget.TimePeriodId,
                                UserId = Budget.UserId,
                            },
                            PlannedExpenses = plannedExpensesRequest,
                            PlannedIncomes = plannedIncomesRequest
                        });
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Fail", ex.Message, "OK");
                return;
            }

            // обновление списка бюджетов
            if (IsEdited)
            {
                var found = Budgets.FirstOrDefault(x => x.Id == Budget.Id);
                int i = Budgets.IndexOf(found);
                Budgets[i] = Budget;
            }
            else
            {
                var plannedExpensesResponse = new List<PlannedExpensesModel>();
                foreach (var plannedExpenses in postResult.PlannedExpenses)
                {
                    plannedExpensesResponse.Add(new PlannedExpensesModel()
                    {
                        Id = plannedExpenses.Id,
                        Sum = (decimal?)plannedExpenses.Sum,
                        ExpenseTypeId = plannedExpenses.ExpenseTypeId,
                        ExpenseType = ExpenseTypes.Where(x => x.Id == plannedExpenses.ExpenseTypeId).FirstOrDefault().Name,
                        BudgetId = plannedExpenses.BudgetId,
                    });
                }

                var plannedIncomesResponse = new List<PlannedIncomesModel>();
                foreach (var plannedIncomes in postResult.PlannedIncomes)
                {
                    plannedIncomesResponse.Add(new PlannedIncomesModel()
                    {
                        Id = plannedIncomes.Id,
                        Sum = (decimal?)plannedIncomes.Sum,
                        IncomeTypeId = plannedIncomes.IncomeTypeId,
                        IncomeType = IncomeTypes.Where(x => x.Id == plannedIncomes.IncomeTypeId).FirstOrDefault().Name,
                        BudgetId = plannedIncomes.BudgetId,
                    });
                }

                Budgets.Add(new BudgetModel()
                {
                    Id = postResult.Budget.Id,
                    Name = postResult.Budget.Name,
                    StartDate = postResult.Budget.StartDate.DateTime,
                    TimePeriodId = postResult.Budget.TimePeriodId,
                    TimePeriod = TimePeriods.Where(x => x.Id == postResult.Budget.TimePeriodId).FirstOrDefault().Name,
                    UserId = postResult.Budget.UserId,
                    PlannedExpenses = plannedExpensesResponse,
                    PlannedIncomes = plannedIncomesResponse,
                });
            }

            await Shell.Current.GoToAsync("..");

        }

        [RelayCommand]
        async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }

        [RelayCommand]
        async Task PlannedExpenses()
        {
            await _popupNavigation.PushAsync(new PlannedExpensesPopup(new PlannedExpensesPopupViewModel(_popupNavigation, Budget)));
        }

        [RelayCommand]
        async Task PlannedIncomes()
        {
            await _popupNavigation.PushAsync(new PlannedIncomesPopup(new PlannedIncomesPopupViewModel(_popupNavigation, Budget)));
        }
    }
}
