using Fodot.CSharp;
using Godot;
using Moon.Class;

namespace Moon.Component;

[GlobalClass, Tool]
public partial class ViewDestroyer : ViewMonitor
{
    [ExportCategory("ViewDestroyer")]
    [Export]
    public bool Disabled { get; set; } = false;
    
    public enum ViewDestroyerProcessCallback { Idle, Physics }
    
    [Export]
    public ViewDestroyerProcessCallback ProcessCallback { get; set; } = ViewDestroyerProcessCallback.Physics;

    public ViewDestroyer() : base()
    {
#if DEBUG
        if (Engine.IsEditorHint()) return;        
#endif
        
        Ready += () =>
        {
            this.AddProcess(Process, ProcessCallback == ViewDestroyerProcessCallback.Physics);
        };
    }

    public void Process()
    {
        if (Disabled) return;

        if (!IsInView()) Monitor.TryQueueFree();
    }

}