using Fracture.Services;

namespace Fracture.ViewModels;

public class ScriptTabViewModel : BaseViewModel
{
    public ScriptTab Tab { get; }

    public ScriptTabViewModel(ScriptTab tab)
    {
        Tab = tab;
    }
}
