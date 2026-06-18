using Godot;

namespace Fodot.Injection;

public partial class NodeComponent : Node, IFScripts
{
    private enum ComponentInjectMode
    {
        Parent,
        Owner
    }
    
    [Export]
    private ComponentInjectMode InjectMode { get ;set; } = ComponentInjectMode.Parent;
    
    public string[] GetFScripts()
        => InjectMode == ComponentInjectMode.Parent ?
            ["component"] : ["owner_component"];
}
