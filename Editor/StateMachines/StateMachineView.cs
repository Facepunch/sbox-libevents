using Editor;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Sandbox.Events.Editor;

public class StateMachineView : GraphicsView
{
	private static Dictionary<Guid, StateMachineView> AllViews { get; } = new Dictionary<Guid, StateMachineView>();

	public static StateMachineView Open( StateMachineComponent stateMachine )
	{
		var guid = stateMachine.Id;

		if ( !AllViews.TryGetValue( guid, out var inst ) )
		{
			var window = StateMachineEditorWindow.AllWindows.LastOrDefault()
			             ?? new StateMachineEditorWindow();

			AllViews[guid] = inst = window.Open( stateMachine );
		}

		inst.Window?.Show();
		inst.Window?.Focus();

		inst.Show();
		inst.Focus();

		inst.Window?.DockManager.RaiseDock( inst.Name );

		return inst;
	}

	public StateMachineComponent StateMachine { get; }

	public StateMachineEditorWindow Window { get; }

	public StateMachineView( StateMachineComponent stateMachine, StateMachineEditorWindow window )
	{
		StateMachine = stateMachine;
		Window = window;
	}
}
