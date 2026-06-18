using Fodot;
using Godot;

namespace Fodot.Injection;

public partial class ParentComponent : Node, IFScripts
{
    public string[] GetFScripts()
        => ["component"];
}
