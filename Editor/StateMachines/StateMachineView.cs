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

	private TransitionItem? _transitionPreview;

	public float GridSize => 32f;

	public StateMachineView( StateMachineComponent stateMachine, StateMachineEditorWindow window )
		: base( null )
	{
		StateMachine = stateMachine;
		Window = window;

		Name = $"View:{stateMachine.Id}";

		WindowTitle = $"{stateMachine.Scene.Name} - {stateMachine.GameObject.Name}";

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
			e.Accepted = GetItemAt( ToScene( e.LocalPosition ) ) is null;
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

		if ( _transitionPreview?.Target is { } target )
		{
			var source = _transitionPreview.Source;

			if ( source.State.Transitions.All( x => x.Target != target.State ) )
			{
				var transition = _transitionPreview.Source.State.Components.Create<TransitionComponent>();

				transition.Target = target.State;

				_transitionPreview.Source.UpdateTransitions();
			}
		}

		_transitionPreview?.Destroy();
		_transitionPreview = null;
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		var scenePos = ToScene( e.LocalPosition );

		if ( _dragging && e.ButtonState.HasFlag( MouseButtons.Left ) )
		{
			if ( _selectionBox == null && !SelectedItems.Any() && !Items.Any( x => x.Hovered ) )
			{
				Add( _selectionBox = new GraphView.SelectionBox( scenePos, this ) );
			}

			if ( _selectionBox != null )
			{
				_selectionBox.EndScene = scenePos;
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
			var delta = scenePos - _lastMouseScenePosition;
			Translate( delta );
			e.Accepted = true;
			Cursor = CursorShape.ClosedHand;
		}
		else
		{
			Cursor = CursorShape.None;
		}

		if ( _transitionPreview.IsValid() )
		{
			_transitionPreview.TargetPosition = scenePos;
			_transitionPreview.Target = GetItemAt( scenePos ) as StateItem;
			_transitionPreview.Layout();
		}

		e.Accepted = true;

		_lastMouseScenePosition = ToScene( e.LocalPosition );
	}

	protected override void OnContextMenu( ContextMenuEvent e )
	{
		var menu = new global::Editor.Menu();
		var scenePos = ToScene( e.LocalPosition );

		if ( GetItemAt( scenePos ) is not null )
		{
			return;
		}

		e.Accepted = true;

		menu.AddOption( "Create New State", action: () =>
		{
			using var _ = StateMachine.Scene.Push();

			var obj = new GameObject( true, "Unnamed" );

			obj.SetParent( StateMachine.GameObject, false );

			var state = obj.Components.Create<StateComponent>();

			state.EditorPosition = scenePos.SnapToGrid( GridSize ) - 64f;

			if ( !StateMachine.CurrentState.IsValid() )
			{
				StateMachine.CurrentState = state;
			}

			var item = new StateItem( this, state );

			_stateItems.Add( state, item );

			Add( item );
		} );

		menu.OpenAtCursor( true );
	}

	[EditorEvent.Frame]
	private void OnFrame()
	{
		var needsUpdate = false;

		foreach ( var (state, item) in _stateItems )
		{
			if ( !state.IsValid )
			{
				needsUpdate = true;
				break;
			}

			foreach ( var transItem in item.Transitions )
			{
				if ( !transItem.Transition.IsValid() )
				{
					needsUpdate = true;
					break;
				}
			}
		}

		if ( needsUpdate )
		{
			UpdateItems();
		}
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

	public void StartCreatingTransition( StateItem source )
	{
		_transitionPreview?.Destroy();

		_transitionPreview = new TransitionItem( null, source, null );

		Add( _transitionPreview );
	}
}
