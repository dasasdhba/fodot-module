using Godot;

namespace Moon;

public partial class ParentComponent : Node, IFScripts
{
    public string[] GetFScripts()
        => ["component"];
}
