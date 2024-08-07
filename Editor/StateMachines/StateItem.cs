using System;
using System.Collections.Generic;
using Editor;

namespace Sandbox.Events.Editor;

public sealed class StateItem : GraphicsItem
{
	private readonly List<TransitionItem> _transitions = new();

	public static Color PrimaryColor { get; } = Color.Parse( "#5C79DB" )!.Value;
	public static Color InitialColor { get; } = Color.Parse( "#BCA5DB" )!.Value;

	public StateMachineView View { get; }
	public StateComponent State { get; }

	public IEnumerable<TransitionItem> Transitions => _transitions;

	public float Radius => 64f;

	public event Action? PositionChanged;

	private bool _rightMousePressed;

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

	public override bool Contains( Vector2 localPos )
	{
		return (LocalRect.Center - localPos).LengthSquared < Radius * Radius;
	}

	protected override void OnPaint()
	{
		var borderColor = Selected 
			? Color.Yellow : Hovered
			? Color.White : Color.White.Darken( 0.125f );

		var fillColor = State.StateMachine.CurrentState == State
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

	public void UpdateTransitions()
	{
		foreach ( var transition in _transitions )
		{
			transition.Destroy();
		}

		_transitions.Clear();

		foreach ( var transition in State.Transitions )
		{
			if ( transition.Target is not { } target ) continue;

			var item = new TransitionItem( transition, this, View.GetStateItem( target ) );
			View.Add( item );

			_transitions.Add( item );
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();

		foreach ( var transition in _transitions )
		{
			transition.Destroy();
		}

		_transitions.Clear();
	}
}
