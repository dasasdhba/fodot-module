using Godot;

namespace Fodot.Injection;

public partial class ParentComponent : Node
{
    public string[] _GetFScripts()
        => ["component"];
}