using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bennewitz.Ninja.AppStem.ViewModels;

/// <summary>
/// The main window's state and commands. No reference to a control: everything here is testable
/// without a UI thread.
/// </summary>
public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(GreetCommand))]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _greeting = "Enter a name and choose Greet.";

    /// <summary>The version this build carries, as the release tag set it.</summary>
    public string Version { get; } =
        typeof(MainWindowViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";

    private bool CanGreet() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanGreet))]
    private void Greet() => Greeting = $"Hello, {Name.Trim()}.";
}
