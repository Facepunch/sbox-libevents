using Editor;
using System.Collections.Generic;
using System;
using System.Linq;
using Editor.NodeEditor;

namespace Sandbox.Events.Editor;

public class StateMachineView : GraphicsView
{
	private static Dictionary<Guid, StateMachineView> AllViews { get; } = new Dictionary<Guid, StateMachineView>();

	public static StateMachineView Open( StateMachineComponent stateMachine )
	{
		var guid = stateMachine.Id;

		if ( !AllViews.TryGetValue( guid, out var inst ) || !inst.IsValid )
		{
			var window = StateMachineEditorWindow.AllWindows.LastOrDefault( x => x.IsValid )
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

	GraphView.SelectionBox _selectionBox;
	private bool _dragging;
	private Vector2 _lastMouseScenePosition;

	private readonly Dictionary<StateComponent, StateItem> _stateItems = new();

	public StateMachineView( StateMachineComponent stateMachine, StateMachineEditorWindow window )
		: base( null )
	{
		StateMachine = stateMachine;
		Window = window;

		Name = $"View:{stateMachine.Id}";

		SetBackgroundImage( "toolimages:/grapheditor/grapheditorbackgroundpattern_shader.png" );

		Antialiasing = true;
		TextAntialiasing = true;
		BilinearFiltering = true;

		SceneRect = new Rect( -100000, -100000, 200000, 200000 );

		HorizontalScrollbar = ScrollbarMode.Off;
		VerticalScrollbar = ScrollbarMode.Off;
		MouseTracking = true;

		UpdateItems();
	}

	protected override void OnClosed()
	{
		base.OnClosed();

		if ( AllViews.TryGetValue( StateMachine.Id, out var view ) && view == this )
		{
			AllViews.Remove( StateMachine.Id );
		}
	}
	protected override void OnWheel( WheelEvent e )
	{
		Zoom( e.Delta > 0 ? 1.1f : 0.90f, e.Position );
		e.Accept();
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.MiddleMouseButton )
		{
			e.Accepted = true;
			return;
		}

		if ( e.RightMouseButton )
		{
			e.Accepted = true;
			return;
		}

		if ( e.LeftMouseButton )
		{
			_dragging = true;
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		base.OnMouseReleased( e );

		_selectionBox?.Destroy();
		_selectionBox = null;
		_dragging = false;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		if ( _dragging && e.ButtonState.HasFlag( MouseButtons.Left ) )
		{
			if ( _selectionBox == null && !SelectedItems.Any() && !Items.Any( x => x.Hovered ) )
			{
				Add( _selectionBox = new GraphView.SelectionBox( ToScene( e.LocalPosition ), this ) );
			}

			if ( _selectionBox != null )
			{
				_selectionBox.EndScene = ToScene( e.LocalPosition );
			}
		}
		else if ( _dragging )
		{
			_selectionBox?.Destroy();
			_selectionBox = null;
			_dragging = false;
		}

		if ( e.ButtonState.HasFlag( MouseButtons.Middle ) ) // or space down?
		{
			var delta = ToScene( e.LocalPosition ) - _lastMouseScenePosition;
			Translate( delta );
			e.Accepted = true;
			Cursor = CursorShape.ClosedHand;
		}
		else
		{
			Cursor = CursorShape.None;
		}

		e.Accepted = true;

		_lastMouseScenePosition = ToScene( e.LocalPosition );
	}

	public void UpdateItems()
	{
		var states = StateMachine.States.ToHashSet();

		foreach ( var (state, item) in _stateItems )
		{
			if ( !states.Contains( state ) )
			{
				item.Destroy();
			}
		}

		foreach ( var state in states )
		{
			if ( !_stateItems.TryGetValue( state, out var item ) )
			{
				item = new StateItem( this, state );
				_stateItems.Add( state, item );
				Add( item );
			}

			item.Position = state.Transform.LocalPosition;
		}

		foreach ( var (_, item) in _stateItems )
		{
			item.UpdateTransitions();
		}
	}

	public StateItem GetStateItem( StateComponent state )
	{
		return _stateItems[state];
	}
}
