﻿using System;
using System.Collections.Generic;
using ApiClient;
using client.Infrastructure;
using client.Model.Models;
using client.View;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mopups.Interfaces;
using System.Collections.ObjectModel;
using client.Model.Enums;

namespace client.ViewModel;

/// <summary>
/// Модель представления меню целей
/// </summary>
public partial class GoalsMenuViewModel : BaseViewModel
{
    private readonly IPopupNavigation _popupNavigation;
    private readonly Client _client;
    private ICollection<IncomeDto> _userIncomes; 
    private ICollection<ExpenseDto> _userExpenses; 

    /// <summary>
    /// Модель представления меню целей
    /// </summary>
    /// <param name="popupNavigation">Навигация попапов</param>
    /// <param name="client">Клиент</param>
    public GoalsMenuViewModel(IPopupNavigation popupNavigation, Client client)
    {
        _popupNavigation = popupNavigation;
        _client = client;
        EventManager.OnUserExit += UserExitHandler;
        EventManager.OnGoalChange += GoalChangeHandler;
    }

    /// <summary>
    /// Заполнение данных после перехода на страницу
    /// </summary>
    public async Task CompleteDataAfterNavigation()
    {
        var userLogin = _client.GetCurrentUserLogin();
        var userDto = await _client.UserAsync(userLogin);
        User = new UserModel
        {
            Id = userDto.Id,
            Login = userDto.Login,
            RefreshToken = userDto.RefreshToken,
            RefreshTokenExpireTime = userDto.RefreshTokenExpiryTime.DateTime
        };

        Goals = [];

        ICollection<GoalDto> result;
        _userIncomes = new List<IncomeDto>();
        _userExpenses = new List<ExpenseDto>();
        
        try
        {
            result = await _client.UserAll3Async(User.Id);
            _userExpenses = await _client.UserAll2Async(User.Id);
            _userIncomes = await _client.UserAll4Async(User.Id);
        }
        catch
        {
            var mainPage = Application.Current!.MainPage;
            if (mainPage is not null)
            {
                await mainPage.DisplayAlert("Fail", Resources.GetUserGoalsFailed, "OK");
            }
                
            return;
        }

        var goalsList = new List<GoalModel>();
        foreach (var goal in result)
        {
            var goalModel = new GoalModel
            {
                Id = goal.Id,
                Name = goal.Name,
                StartDate = goal.StartDate.DateTime,
                EndDate = goal.EndDate.GetValueOrDefault(goal.StartDate.DateTime).DateTime,
                Sum = (decimal?)goal.Sum,
                UserId = goal.UserId
            };

            SetGoalStatus(goalModel);
            goalsList.Add(goalModel);
        }

        Goals = new ObservableCollection<GoalModel>(goalsList.OrderByDescending(x => x.StartDate).ToList());
    } 
      
    /// <summary>
    /// Цели
    /// </summary>
    [ObservableProperty] private ObservableCollection<GoalModel> _goals;

    /// <summary>
    /// Пользователь
    /// </summary>
    [ObservableProperty] private UserModel? _user;
    
    /// <summary>
    /// Команда удаления цели
    /// </summary>
    /// <param name="goal">Цель</param>
    [RelayCommand]
    private async Task DeleteGoal(GoalModel goal)
    {
        try
        {
            await _client.GoalDELETEAsync(goal.Id);
        }
        catch
        {
            var mainPage = Application.Current!.MainPage;
            if (mainPage is not null)
            {
                await mainPage.DisplayAlert("Fail", Resources.DeleteGoalFailed, "OK");
            }
                
            return;
        }

        Goals.Remove(goal);
    }

    /// <summary>
    /// Команда добавления цели
    /// </summary>
    [RelayCommand]
    private async Task AddGoal()
    {
        if (User is not null)
        {
            GoalModel goal = new()
            {
                UserId = User.Id,
            };

            await _popupNavigation.PushAsync(new GoalsPopup(
                new GoalsPopupViewModel(_popupNavigation, _client, goal, Goals, false)));
        }
    }

    /// <summary>
    /// Команда редактирования цели
    /// </summary>
    /// <param name="goal">Цель</param>
    /// <returns></returns>
    [RelayCommand]
    private Task EditGoal(GoalModel goal) => _popupNavigation.PushAsync(new GoalsPopup(
        new GoalsPopupViewModel(_popupNavigation, _client, goal, Goals, true)));
    
    /// <summary>
    /// Команда перехода на страницу статистики целей
    /// </summary>
    [RelayCommand]
    private async Task GoToGoalsStatistics()
    {
        if (User is not null)
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "Goals", Goals }
            };

            await Shell.Current.GoToAsync($"{nameof(GoalsStatisticsPage)}", navigationParameter);
        }
    }
        
    /// <summary>
    /// Обработчик выхода пользователя
    /// </summary>
    private Task UserExitHandler()
    {
        User = null;
        Goals = [];
        return Task.CompletedTask;
    }

    /// <summary>
    /// Устанавливает статус цели
    /// </summary>
    /// <param name="goal">Цель</param>
    private void SetGoalStatus(GoalModel goal)
    {
        if (DateTime.Now < goal.StartDate)
        {
            goal.IsCompleted = false;
            goal.Status = GoalStatuses.NotStarted;
            
            return;
        }
        
        if (DateTime.Now >= goal.StartDate && DateTime.Now <= goal.EndDate)
        {
            goal.IsCompleted = false;
            goal.Status = GoalStatuses.InProgress;
            
            return;
        }

        var incomesSumInGoalPeriod = _userIncomes
            .Where(income => income.Date >= goal.StartDate && income.Date <= goal.EndDate)
            .Sum(income => income.Value);

        var expensesSumInGoalPeriod = -_userExpenses
            .Where(expense => expense.Date >= goal.StartDate && expense.Date <= goal.EndDate)
            .Sum(expense => expense.Value);
        
        var balance = incomesSumInGoalPeriod - expensesSumInGoalPeriod;

        if ((decimal)balance >= goal.Sum.GetValueOrDefault(0))
        {
            goal.IsCompleted = true;
            goal.Status = GoalStatuses.Completed;
            
            return;
        }

        goal.IsCompleted = false;
        goal.Status = GoalStatuses.Failed;
    }

    private void GoalChangeHandler(GoalModel goal) => SetGoalStatus(goal);
}