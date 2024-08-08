using System;
using System.Linq;
using Editor;
using Editor.NodeEditor;

namespace Sandbox.Events.Editor;

public sealed class StateItem : GraphicsItem, IContextMenuSource, IDeletable
{
	public static Color PrimaryColor { get; } = Color.Parse( "#5C79DB" )!.Value;
	public static Color InitialColor { get; } = Color.Parse( "#BCA5DB" )!.Value;

	public StateMachineView View { get; }
	public StateComponent State { get; }

	public float Radius => 64f;

	public event Action? PositionChanged;

	private bool _rightMousePressed;

	private int _lastHash;

	public StateItem( StateMachineView view, StateComponent state )
	{
		View = view;
		State = state;

		Size = new Vector2( Radius * 2f, Radius * 2f );
		Position = state.EditorPosition;

		Movable = true;
		Selectable = true;
		HoverEvents = true;
	}

	public override Rect BoundingRect => base.BoundingRect.Grow( 16f );

	public override bool Contains( Vector2 localPos )
	{
		return (LocalRect.Center - localPos).LengthSquared < Radius * Radius;
	}

	protected override void OnPaint()
	{
		var borderColor = Selected 
			? Color.Yellow : Hovered
			? Color.White : Color.White.Darken( 0.125f );

		var fillColor = State.StateMachine?.InitialState == State
			? InitialColor
			: PrimaryColor;

		fillColor = fillColor
			.Lighten( Selected ? 0.5f : Hovered ? 0.25f : 0f )
			.Desaturate( Selected ? 0.5f : Hovered ? 0.25f : 0f );

		Paint.SetBrushRadial( LocalRect.Center - LocalRect.Size * 0.125f, Radius * 1.5f, fillColor.Lighten( 0.5f ), fillColor.Darken( 0.75f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		Paint.SetPen( borderColor, Selected || Hovered ? 3f : 2f );
		Paint.SetBrushRadial( LocalRect.Center, Radius, 0.75f, Color.Black.WithAlpha( 0f ), 1f, Color.Black.WithAlpha( 0.25f ) );
		Paint.DrawCircle( Size * 0.5f, Size );

		if ( State.StateMachine?.CurrentState == State )
		{
			Paint.ClearBrush();
			Paint.DrawCircle( Size * 0.5f, Size + 8f );
		}

		Paint.ClearBrush();
		Paint.SetFont( "roboto", 12f, 600 );
		Paint.SetPen( Color.Black.WithAlpha( 0.5f ) );
		Paint.DrawText( new Rect( 2f, 2f, Size.x, Size.y ), State.GameObject.Name );

		Paint.SetPen( borderColor );
		Paint.DrawText( new Rect( 0f, 0f, Size.x, Size.y ), State.GameObject.Name );
	}

	protected override void OnMousePressed( GraphicsMouseEvent e )
	{
		base.OnMousePressed( e );

		if ( e.RightMouseButton )
		{
			_rightMousePressed = true;

			e.Accepted = true;
		}
	}

	protected override void OnMouseReleased( GraphicsMouseEvent e )
	{
		base.OnMouseReleased( e );

		if ( e.RightMouseButton && _rightMousePressed )
		{
			_rightMousePressed = false;

			e.Accepted = true;
		}
	}

	protected override void OnMouseMove( GraphicsMouseEvent e )
	{
		if ( _rightMousePressed && !Contains( e.LocalPosition ) )
		{
			_rightMousePressed = false;

			View.StartCreatingTransition( this );
		}

		base.OnMouseMove( e );
	}

	private void UpdateTransitions()
	{
		foreach ( var transition in State.Transitions )
		{
			View.GetTransitionItem( transition )?.Update();
		}
	}

	protected override void OnHoverEnter( GraphicsHoverEvent e )
	{
		base.OnHoverEnter( e );
		UpdateTransitions();
	}

	protected override void OnHoverLeave( GraphicsHoverEvent e )
	{
		base.OnHoverLeave( e );
		UpdateTransitions();
	}

	protected override void OnSelectionChanged()
	{
		base.OnSelectionChanged();
		UpdateTransitions();
	}

	public void OnContextMenu( ContextMenuEvent e )
	{
		e.Accepted = true;
		Selected = true;

		var menu = new global::Editor.Menu();

		if ( State.StateMachine.InitialState != State )
		{
			menu.AddOption( "Make Initial State", "start", action: () =>
			{
				State.StateMachine.InitialState = State;
				Update();
			} );

			menu.AddSeparator();
		}

		menu.AddMenu( "Rename State", "edit" ).AddLineEdit( "Rename", State.GameObject.Name, onSubmit: value =>
		{
			State.GameObject.Name = value;
			Update();
		}, autoFocus: true );

		menu.AddOption( "Delete State", "delete", action: Delete );

		menu.OpenAtCursor( true );
	}

	protected override void OnMoved()
	{
		State.EditorPosition = Position.SnapToGrid( View.GridSize );
		SceneEditorSession.Active.Scene.EditLog( "State Moved", State );

		UpdatePosition();
	}

	public void UpdatePosition()
	{
		Position = State.EditorPosition;

		PositionChanged?.Invoke();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		var transitions = View.Items.OfType<TransitionItem>()
			.Where( x => x.Source == this || x.Target == this )
			.ToArray();

		foreach ( var transition in transitions )
		{
			transition.Destroy();
		}
	}

	public void Delete()
	{
		if ( State.StateMachine.InitialState == State )
		{
			State.StateMachine.InitialState = null;
		}

		var transitions = View.Items.OfType<TransitionItem>()
			.Where( x => x.Source == this || x.Target == this )
			.ToArray();

		foreach ( var transition in transitions )
		{
			transition.Delete();
		}

		State.GameObject.Destroy();
		Destroy();
	}

	public void Frame()
	{
		var hash = HashCode.Combine( State.StateMachine?.InitialState == State, State.StateMachine?.CurrentState == State );
		if ( hash == _lastHash ) return;

		_lastHash = hash;
		Update();
	}
}
