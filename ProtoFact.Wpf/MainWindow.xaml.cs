using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using Ninject;
using ProtoFact.Abstractions;
using ProtoFact.Control;
using ProtoFact.Engine;
using ProtoFact.Infrastructure;
using ProtoFact.Scenarios;
using ProtoFact.Wpf.ViewModels;

using EngineRunner = ProtoFact.Engine.Engine;

namespace ProtoFact.Wpf;

/// <summary>
/// Runs the same simulation as the console app, driven by a single-threaded
/// DispatcherTimer on the UI thread (matching the console's loop model), so
/// <see cref="IInventory"/> mutations from both the timer tick and the
/// "Add" buttons stay safely serialized without locks.
/// </summary>
public partial class MainWindow : Window
{
    private readonly DemoScenario _scenario;
    private readonly IInventory _inventory;
    private readonly IProductionController _controller;
    private readonly IProductionTreeBuilder _treeBuilder;
    private readonly EngineRunner _engine;
    private readonly DispatcherTimer _timer;

    private readonly ObservableCollection<ItemRowViewModel> _rows = new();
    private readonly ObservableCollection<RawResourceViewModel> _rawResources = new();

    public MainWindow()
    {
        InitializeComponent();

        var kernel = new StandardKernel(new BindingsModule());

        var logger = kernel.Get<ILogger>();
        _inventory = kernel.Get<IInventory>();
        var adaptiveController = kernel.Get<IAdaptiveController>();
        var bufferStrategy = kernel.Get<IBufferStrategy>();
        var rateSolver = kernel.Get<IRateSolver>();
        _treeBuilder = kernel.Get<IProductionTreeBuilder>();

        _scenario = DemoScenario.Create();

        var validation = kernel.Get<IModelValidator>().Validate(_scenario.Recipes);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Recipe model failed validation: {string.Join("; ", validation.Errors)}");
        }

        _inventory.Add(_scenario.InitialStock);

        var controller = new ProductionController(rateSolver,
                                                  adaptiveController,
                                                  bufferStrategy,
                                                  _inventory,
                                                  logger);

        foreach (var goal in _scenario.Goals)
        {
            controller.AddGoal(goal);
        }

        _controller = controller;
        _engine = new EngineRunner(controller.Processors, new WpfTimeProvider());

        foreach (var item in _scenario.TrackedItems)
        {
            _rows.Add(new ItemRowViewModel(item));
        }

        foreach (var item in _scenario.RawItems)
        {
            _rawResources.Add(new RawResourceViewModel(item, _inventory));
        }

        OverviewGrid.ItemsSource = _rows;
        RawResourcesList.ItemsSource = _rawResources;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100),
        };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();

        Closed += (_, _) => _timer.Stop();

        RefreshRows();
    }

    private void Tick()
    {
        _controller.Tick(_scenario.Recipes);
        _engine.Tick();

        RefreshRows();
    }

    private void RefreshRows()
    {
        var goals = _controller.GetGoals().ToDictionary(g => g.Target);
        foreach (var row in _rows)
        {
            row.Stock = _inventory.Get(row.Item);
            row.Utilization = _controller.GetUtilization(row.Item);
            row.Throughput = _controller.GetThroughput(row.Item);
            row.MaxThroughput = _controller.GetMaxThroughput(row.Item, _scenario.Recipes);
            row.ProcessorCount = _controller.Processors.Count(p => p.Recipe.Output.Item.Equals(row.Item));

            goals.TryGetValue(row.Item, out var goal);
            var target = goal?.TargetRate ?? 0;
            row.TargetRate = target;
            row.Delta = target > 0 ? row.Throughput - target : 0;
            row.UpdateStatus();
        }

        var goalSummary = _scenario.Goals.Any()
            ? string.Join(", ", _scenario.Goals.Select(g => $"{g.Target.Name}:{g.TargetRate:F1}/s"))
            : "None";

        var bottlenecks = _controller.GetTopBottlenecks(3).ToList();
        var bottleneckSummary = bottlenecks.Any()
            ? string.Join(", ", bottlenecks.Select(b => $"⚠️ {b.Item.Name} ({b.Severity:P0})"))
            : "✅ None";

        var metrics = _controller.GetSystemMetrics();

        GoalsText.Text = $"🎯 Goals: {goalSummary}";
        BottlenecksText.Text = $"Bottlenecks: {bottleneckSummary}";
        SystemText.Text = $"📈 System: Util {metrics.Utilization:P0} | Idle {metrics.IdleFraction:P0}";

        RefreshTree();
    }

    private void RefreshTree()
    {
        var trees = _treeBuilder.BuildTrees(_scenario.Goals, _scenario.Recipes, _inventory);
        RecipeTreeView.ItemsSource = trees;
    }

    /// <summary>
    /// Wall-clock time provider for the WPF app's DispatcherTimer-driven loop.
    /// </summary>
    private sealed class WpfTimeProvider : ITimeProvider
    {
        private DateTime _last = DateTime.UtcNow;

        public double DeltaTime
        {
            get
            {
                var now = DateTime.UtcNow;
                var delta = (now - _last).TotalSeconds;
                _last = now;
                return delta;
            }
        }
    }
}
