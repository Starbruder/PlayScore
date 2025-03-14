﻿using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot;
using PlayScore.Models;
using PlayScore.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PlayScore;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DatabaseManager _databaseManager;

    private readonly MoonphaseService _moonphaseService;
    private readonly GameService _gameService;

    public ObservableCollection<GameModel> Games { get; } = [];
    public MainWindow(DatabaseManager databaseManager, MoonphaseService moonphaseService, GameService gameService)
    {
        InitializeComponent();

        WindowState = WindowState.Normal;

        _databaseManager = databaseManager;
        _moonphaseService = moonphaseService;
        _gameService = gameService;

        // Create the plot
        MoonPhasePlot.Model = CreatePlotModel();
    }

    private async void GetMoonphase(object sender, RoutedEventArgs e)
    {
        string date = DateTextBox.Text;

        // Example: Rostock 
        var latitude = 54.0924;
        var longitude = 12.1407;

        var moonPhaseData = await _moonphaseService.GetMoonPhaseAsync(date, latitude, longitude);

        if (moonPhaseData == null)
        {
            MessageBox.Show("Failed to retrieve moon phase data. Please check your connection or try again.");
            return;
        }

        var translator = new MoonphaseTranslator();
        MoonPhaseTextBlock.Text = $"Mondphase: {translator.Translate(moonPhaseData.MoonPhase)}";

        // Trying fetching Image instead
        //imgTitle.Source = new BitmapImage(new(@"pack://application:,,,/Images/phases/8_FullMoon.png"));

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri("pack://application:,,,/Images/phases/8_FullMoon.png", UriKind.Absolute);
        bitmap.EndInit();
        imgTitle.Source = bitmap;

    }

    private async void GetGames(object sender, RoutedEventArgs e)
    {
        string date = DateTextBox.Text;
        GamesListBox.ItemsSource = Games;

        var gameData = await _gameService.GetGamesByReleaseDateAsync(date);

        if (gameData == null)
        {
            MessageBox.Show("Failed to retrieve game data. Please check your connection or try again.");
            return;
        }

        Games.Clear();
        gameData.ForEach(Games.Add);
    }

    public void SaveGamesToDatabase(object sender, RoutedEventArgs e)
    {
        var games = (ObservableCollection<GameModel>)GamesListBox.ItemsSource;
        foreach (var game in games)
        {
            if (game.Rating > 0)
            {
                _databaseManager.AddGameToSpieleTable(game);
            }
        }
    }

    private PlotModel CreatePlotModel()
    {
        // Generate sample data, still need method for getting the data from our database
        Dictionary<string, double> ratings = new Dictionary<string, double>
            {
                { "New Moon", 7.5 },
                { "First Quarter", 8.2 },
                { "Full Moon", 9.1 },
                { "Last Quarter", 6.8 }
            };

        var plotModel = new PlotModel { Title = "Game Ratings vs Moon Phases" };

        // Define the Y-Axis (Categories for Moon Phases)
        var categoryAxis = new CategoryAxis
        {
            Position = AxisPosition.Left, // BarChart uses Left Y-Axis for categories
            Title = "Moon Phase"
        };

        foreach (var phase in ratings.Keys)
            categoryAxis.Labels.Add(phase);

        plotModel.Axes.Add(categoryAxis);

        // Define the X-Axis (Ratings)
        var valueAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Title = "Average Rating",
            Minimum = 0,
            Maximum = 10
        };
        plotModel.Axes.Add(valueAxis);

        // Add BarSeries
        var barSeries = new BarSeries { LabelPlacement = LabelPlacement.Inside };

        foreach (var rating in ratings.Values)
            barSeries.Items.Add(new BarItem { Value = rating });

        plotModel.Series.Add(barSeries);

        return plotModel;
    }
}