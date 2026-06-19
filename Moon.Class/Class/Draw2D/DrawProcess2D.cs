using System;
using System.Collections.Generic;

namespace Moon.Class;

/// <summary>
/// Manage drawing task with process functions.
/// </summary>
public partial class DrawProcess2D : Draw2D
{    
    private List<Func<double, bool>> QueuedTasks = [];
    public int GetQueuedTaskCount() => QueuedTasks.Count;
    
    public void AddDrawProcess(Func<double, bool> process)
        => QueuedTasks.Add(process);

    protected override bool DrawProcess(double delta)
    {
        List<Func<double, bool>> finished = [];
    
        foreach (var task in QueuedTasks)
        {
            ClearDrawSettings();
            
            var result = task(delta);
            if (result)
            {
                finished.Add(task);
            }
        }
        
        foreach (var task in finished)
        {
            QueuedTasks.Remove(task);
        }
        
        return true;
    }
}