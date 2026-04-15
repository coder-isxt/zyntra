using Zyntra.Services;

namespace Zyntra.ViewModels;

public class ScriptTabViewModel : BaseViewModel
{
    public ScriptTab Tab { get; }

    public ScriptTabViewModel(ScriptTab tab)
    {
        Tab = tab;
    }
}
