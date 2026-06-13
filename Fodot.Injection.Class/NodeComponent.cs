using Godot;

namespace Fodot.Injection;

public partial class NodeComponent : Node
{
    private enum ComponentInjectMode
    {
        Parent,
        Owner
    }
    
    [Export]
    private ComponentInjectMode InjectMode { get ;set; } = ComponentInjectMode.Parent;
    
    public string[] _GetFScripts()
        => InjectMode == ComponentInjectMode.Parent ?
            ["component"] : ["owner_component"];
}