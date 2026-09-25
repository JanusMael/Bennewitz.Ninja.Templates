using System.Reflection;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Bennewitz.Ninja.AppStem.ViewModels;
using Bennewitz.Ninja.AppStem.Views;
using Semi.Avalonia;

namespace Bennewitz.Ninja.AppStem.Tests;

/// <summary>
/// The main window, driven through its controls on the headless platform, and its view model
/// without one.
/// </summary>
/// <remarks>
/// ⛔ The body handed to <c>Dispatch</c> is synchronous. An <c>async</c> lambda there becomes a task
/// nothing awaits, and the test passes whatever the body does.
/// </remarks>
public sealed class MainWindowTests
{
    private static HeadlessUnitTestSession Session =>
        HeadlessUnitTestSession.GetOrStartForAssembly(Assembly.GetExecutingAssembly());

    [Fact]
    public void Greet_is_unavailable_until_a_name_is_entered()
    {
        MainWindowViewModel viewModel = new();
        Assert.False(viewModel.GreetCommand.CanExecute(null));

        viewModel.Name = "   ";
        Assert.False(viewModel.GreetCommand.CanExecute(null));

        viewModel.Name = "Ada";
        Assert.True(viewModel.GreetCommand.CanExecute(null));
    }

    /// <summary>
    /// The About panel shows a version shaped like one. Shape, because the obvious attribute,
    /// <c>AssemblyInformationalVersion</c>, is AutoVersioning's "Built with ♥ …".
    /// </summary>
    [Fact]
    public void The_version_is_the_release_version()
    {
        Assert.Matches(@"^\d+\.\d+\.\d+", new MainWindowViewModel().Version);
    }

    [Fact]
    public void Greet_greets_the_trimmed_name()
    {
        MainWindowViewModel viewModel = new() { Name = "  Ada " };

        viewModel.GreetCommand.Execute(null);

        Assert.Equal("Hello, Ada.", viewModel.Greeting);
    }

    [Fact]
    public Task Typing_a_name_and_pressing_Greet_shows_the_greeting()
    {
        return Session.Dispatch(() =>
        {
            MainWindowViewModel viewModel = new();
            MainWindow window = new() { DataContext = viewModel };
            window.Show();

            // Found by AutomationId, the handle an automation client uses, rather than by x:Name.
            TextBox input = Find<TextBox>(window, "NameInput");
            Button greet = Find<Button>(window, "GreetButton");
            TextBlock greeting = Find<TextBlock>(window, "GreetingText");

            input.Text = "Ada";
            Assert.Equal("Ada", viewModel.Name);
            Assert.True(greet.IsEffectivelyEnabled);

            greet.Command!.Execute(greet.CommandParameter);
            window.UpdateLayout();

            Assert.Equal("Hello, Ada.", greeting.Text);
            window.Close();
        }, CancellationToken.None);
    }

    /// <summary>
    /// The window renders under the app's own theme. Without it every control is template-less, and
    /// a test that finds its controls would still pass against a window no user would recognise.
    /// </summary>
    [Fact]
    public Task The_window_renders_under_the_Semi_theme()
    {
        return Session.Dispatch(() =>
        {
            Assert.Contains(Application.Current!.Styles, style => style is SemiTheme);

            MainWindow window = new() { DataContext = new MainWindowViewModel() };
            window.Show();

            Button greet = Find<Button>(window, "GreetButton");
            Assert.NotNull(greet.Template);
            Assert.NotEmpty(greet.GetVisualChildren());
            window.Close();
        }, CancellationToken.None);
    }

    private static T Find<T>(Window window, string automationId)
        where T : Control
    {
        T[] matches =
        [
            .. window.GetVisualDescendants()
                .OfType<T>()
                .Where(control => AutomationProperties.GetAutomationId(control) == automationId),
        ];

        return Assert.Single(matches);
    }
}
