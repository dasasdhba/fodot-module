using Godot;
using Godot.Collections;
using Moon.Class;

namespace Moon.Component;

[GlobalClass, Tool]
public partial class ViewMonitor : Node
{
	[Export(PropertyHint.NodePathValidTypes, "CanvasItem")]
	public NodePath MonitorNode { get; set; } = "..";
	
	public enum ViewArea { Current, All }
	
	[Export]
	public ViewArea Area { get; set; } = ViewArea.Current;
	
	public enum MonitorMode { Direction, Manual, Total }

	[ExportGroup("ViewSettings")]
	[Export]
	public MonitorMode Mode
	{
		get => _mode;
		set
		{
			_mode = value;
			NotifyPropertyListChanged();
		}    
	}
	
	private MonitorMode _mode;
	
	[Export]
	public Vector2 Direction { get ;set; } = Vector2.Down;
	
	[Export]
	public float Range { get; set; } = 48f;
	
	[Export]
	public bool MonitorUp { get; set; } = true;
	
	[Export]
	public float UpRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorDown { get; set; } = true;
	
	[Export]
	public float DownRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorLeft { get; set; } = true;
	
	[Export]
	public float LeftRange { get; set; } = 48f;
	
	[Export]
	public bool MonitorRight { get; set; } = true;
	
	[Export]
	public float RightRange { get; set; } = 48f;
	
	[Signal]
	public delegate void InvokeInViewEventHandler();
	
	[Signal]
	public delegate void InvokeOutViewEventHandler();

#if DEBUG
	
	public override void _ValidateProperty(Dictionary property)
	{
		if (
			(Mode == MonitorMode.Direction && (string)property["name"] is
				"UpRange" or "DownRange" or "LeftRange" or "RightRange" or 
				"MonitorUp" or "MonitorDown" or "MonitorLeft" or "MonitorRight") ||
			(Mode == MonitorMode.Manual && (string)property["name"] is 
				"Direction" or "Range") ||
			(Mode == MonitorMode.Total && (string)property["name"] is
				"UpRange" or "DownRange" or "LeftRange" or "RightRange" or
				"MonitorUp" or "MonitorDown" or "MonitorLeft" or "MonitorRight" or  
				"Direction")
		)
		{
			property["usage"] = (uint)PropertyUsageFlags.ReadOnly;
		}
	}
	
#endif

	protected CanvasItem Monitor { get; private set; }

	public ViewMonitor() : base()
	{
	#if DEBUG
		if (Engine.IsEditorHint()) return;
	#endif	
	
		Ready += () => Monitor = GetNode<CanvasItem>(MonitorNode);
	}
	
	private bool IsInArea() => Area == ViewArea.Current ?
		Monitor.IsInView(Range) : Monitor.IsInViewRegion(Range);
	
	private bool IsInAreaDir() => Area == ViewArea.Current ?
		Monitor.IsInViewDir(Direction, Range) :
		Monitor.IsInViewRegionDir(Direction, Range);
		
	private bool IsInAreaTop() => Area == ViewArea.Current ?
		Monitor.IsInViewTop(UpRange) :
		Monitor.IsInViewRegionTop(UpRange);
		
	private bool IsInAreaBottom() => Area == ViewArea.Current ?
		Monitor.IsInViewBottom(DownRange) :
		Monitor.IsInViewRegionBottom(DownRange);
		
	private bool IsInAreaLeft() => Area == ViewArea.Current ?
		Monitor.IsInViewLeft(LeftRange) :
		Monitor.IsInViewRegionLeft(LeftRange);
	
	private bool IsInAreaRight() => Area == ViewArea.Current ?
		Monitor.IsInViewRight(RightRange) :
		Monitor.IsInViewRegionRight(RightRange);

	public bool IsInView()
	{
		if (Mode == MonitorMode.Total)
		{
			return IsInArea();
		}
	
		if (Mode == MonitorMode.Direction)
		{
			return IsInAreaDir();
		}

		return (!MonitorUp || IsInAreaTop())
			&& (!MonitorDown || IsInAreaBottom())
			&& (!MonitorLeft || IsInAreaLeft())
			&& (!MonitorRight || IsInAreaRight());
	}

	public void Invoke()
	{
		if (IsInView()) EmitSignal(SignalName.InvokeInView);
		else EmitSignal(SignalName.InvokeOutView);
	}
}