using Editor;
using System.Collections.Generic;
using System;
using System.Linq;
using Editor.NodeEditor;
using static Sandbox.PhysicsContact;

namespace Sandbox.Events.Editor;

public interface IContextMenuSource
{
	void OnContextMenu( ContextMenuEvent e );
}

public interface IDeletable
{
	void Delete();
}

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
	private readonly Dictionary<TransitionComponent, TransitionItem> _transitionItems = new();

	private TransitionItem? _transitionPreview;
	private bool _wasDraggingTransition;

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

				AddTransitionItem( transition );
			}
		}

		if ( _transitionPreview is not null )
		{
			_transitionPreview?.Destroy();
			_transitionPreview = null;

			_wasDraggingTransition = true;

			e.Accepted = true;
		}
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
		if ( _wasDraggingTransition )
		{
			return;
		}

		var menu = new global::Editor.Menu();
		var scenePos = ToScene( e.LocalPosition );

		if ( GetItemAt( scenePos ) is IContextMenuSource source )
		{
			source.OnContextMenu( e );

			if ( e.Accepted ) return;
		}

		e.Accepted = true;

		// var createMenu = menu.AddMenu( "Create New State", "add" );

		menu.AddLineEdit( "New State", placeholder: "Name", autoFocus: true, onSubmit: name =>
		{
			using var _ = StateMachine.Scene.Push();

			var obj = new GameObject( true, name ?? "Unnamed" );

			obj.SetParent( StateMachine.GameObject, false );

			var state = obj.Components.Create<StateComponent>();

			state.EditorPosition = scenePos.SnapToGrid( GridSize ) - 64f;

			if ( !StateMachine.InitialState.IsValid() )
			{
				StateMachine.InitialState = state;
			}

			AddStateItem( state );
		} );

		menu.OpenAtCursor( true );
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		base.OnKeyPress( e );

		if ( e.Key == KeyCode.Delete )
		{
			e.Accepted = true;

			var deletable = SelectedItems
				.OfType<IDeletable>()
				.ToArray();

			foreach ( var item in deletable )
			{
				item.Delete();
			}
		}
	}

	[EditorEvent.Frame]
	private void OnFrame()
	{
		_wasDraggingTransition = false;

		var needsUpdate = false;

		foreach ( var (state, item) in _stateItems )
		{
			if ( !state.IsValid )
			{
				needsUpdate = true;
				break;
			}
		}

		foreach ( var (transition, item) in _transitionItems )
		{
			if ( !transition.IsValid )
			{
				needsUpdate = true;
				break;
			}
		}

		if ( needsUpdate )
		{
			UpdateItems();
		}
		else
		{
			foreach ( var item in _stateItems.Values )
			{
				item.Frame();
			}
		}
	}

	public void UpdateItems()
	{
		ItemHelper<StateComponent, StateItem>.Update( this, StateMachine.States, _stateItems, AddStateItem );
		ItemHelper<TransitionComponent, TransitionItem>.Update( this, StateMachine.States.SelectMany( x => x.Transitions ), _transitionItems, AddTransitionItem );
	}

	private void AddStateItem( StateComponent state )
	{
		var item = new StateItem( this, state );
		_stateItems.Add( state, item );
		Add( item );
	}

	private void AddTransitionItem( TransitionComponent transition )
	{
		var source = GetStateItem( transition.Source );
		var target = GetStateItem( transition.Target );

		if ( source is null || target is null ) return;

		var item = new TransitionItem( transition, source, target );
		_transitionItems.Add( transition, item );
		Add( item );
	}

	public StateItem? GetStateItem( StateComponent state )
	{
		return _stateItems!.GetValueOrDefault( state );
	}

	public TransitionItem? GetTransitionItem( TransitionComponent transition )
	{
		return _transitionItems!.GetValueOrDefault( transition );
	}

	public void StartCreatingTransition( StateItem source )
	{
		_transitionPreview?.Destroy();

		_transitionPreview = new TransitionItem( null, source, null );
		_transitionPreview.TargetPosition = source.Center;

		Add( _transitionPreview );
	}

	private static class ItemHelper<TComponent, TItem>
		where TComponent : Component
		where TItem : GraphicsItem
	{
		[ThreadStatic] private static HashSet<TComponent> SourceSet;
		[ThreadStatic] private static List<TComponent> ToRemove;

		public static void Update( GraphicsView view, IEnumerable<TComponent> source, Dictionary<TComponent, TItem> dict, Action<TComponent> add )
		{
			SourceSet ??= new HashSet<TComponent>();
			SourceSet.Clear();

			ToRemove ??= new List<TComponent>();
			ToRemove.Clear();

			foreach ( var component in source )
			{
				SourceSet.Add( component );
			}

			foreach ( var (state, item) in dict )
			{
				if ( !SourceSet.Contains( state ) )
				{
					item.Destroy();
					ToRemove.Add( state );
				}
			}

			foreach ( var removed in ToRemove )
			{
				dict.Remove( removed );
			}

			foreach ( var component in SourceSet )
			{
				if ( !dict.ContainsKey( component ) )
				{
					add( component );
				}
			}
		}
	}
}
