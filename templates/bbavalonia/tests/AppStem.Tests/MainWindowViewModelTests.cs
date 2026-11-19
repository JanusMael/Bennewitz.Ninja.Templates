using Bennewitz.Ninja.AppStem.ViewModels;

namespace Bennewitz.Ninja.AppStem.Tests;

/// <summary>
/// The main window's view model, with no headless session. The window itself, driven through its
/// controls, is <c>MainWindowTests</c> in AppStem.Headless.Tests.
/// </summary>
public sealed class MainWindowViewModelTests
{
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
}
