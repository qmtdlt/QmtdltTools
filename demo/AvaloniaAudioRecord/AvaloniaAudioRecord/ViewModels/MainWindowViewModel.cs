using ReactiveUI;
using System.Threading.Tasks;

namespace AvaloniaAudioRecord.ViewModels;

public partial class MainWindowViewModel : ReactiveObject
{
    public MainWindowViewModel()
    {
        Task.Run(async () =>
        {
            while (true)
            {
                Greeting = "Welcome to Avalonia!";
                await Task.Delay(1000);
                Greeting = "asdfadsf";
                await Task.Delay(1000);
            }
        });
    }
    //public string Greeting { get; set; } = "Welcome to Avalonia!";

    private string? _greeting; // This is our backing field for Name

    public string? Greeting
    {
        get
        {
            return _greeting;
        }
        set
        {
            // We can use "RaiseAndSetIfChanged" to check if the value changed and automatically notify the UI
            this.RaiseAndSetIfChanged(ref _greeting, value);
        }
    }
}