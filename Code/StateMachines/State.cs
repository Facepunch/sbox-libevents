using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sandbox.Events;

/// <summary>
/// Marks a <see cref="GameObject"/> as a state in a state machine. There must be a
/// <see cref="StateMachineComponent"/> on an ancestor object for this to function.
/// The object containing this state (and all ancestors) will be enabled when the state
/// machine transitions to this state, and will disable again when this state is exited.
/// States may be nested within each other.
/// </summary>
[Title( "State" ), Icon( "circle" ), Category( "State Machines" )]
public sealed class StateComponent : Component
{
	private StateMachineComponent? _stateMachine;

	/// <summary>
	/// Which state machine does this state belong to?
	/// </summary>
	public StateMachineComponent StateMachine =>
		_stateMachine ??= Components.GetInAncestorsOrSelf<StateMachineComponent>();

	/// <summary>
	/// Which state is this nested in, if any?
	/// </summary>
	public StateComponent? Parent => Components.GetInAncestors<StateComponent>( true );

	public IEnumerable<TransitionComponent> Transitions => Components.GetAll<TransitionComponent>( FindMode.EverythingInSelf );

	/// <summary>
	/// Event dispatched on the owner when this state is entered.
	/// </summary>
	[Property, KeyProperty]
	public event Action? OnEnterState;

	/// <summary>
	/// Event dispatched on the owner while this state is active.
	/// </summary>
	[Property, KeyProperty]
	public event Action? OnUpdateState;

	/// <summary>
	/// Event dispatched on the owner when this state is exited.
	/// </summary>
	[Property, KeyProperty]
	public event Action? OnLeaveState;

	[Property, Hide]
	public Vector2 EditorPosition { get; set; }

	private TimeSince _sinceEnter;

	/// <summary>
	/// How long since we entered this state?
	/// </summary>
	[ActionGraphInclude]
	public float Time => Enabled ? _sinceEnter : 0f;

	private TransitionComponent? _nextTransition;

	internal void Enter( bool dispatch )
	{
		Enabled = true;

		_sinceEnter = 0f;
		_nextTransition = null;

		if ( dispatch )
		{
			OnEnterState?.Invoke();
			GameObject.Dispatch( new EnterStateEvent( this ) );

			TestTransitions();
		}
	}

	internal void Update()
	{
		OnUpdateState?.Invoke();
		Scene.Dispatch( new UpdateStateEvent( this ) );

		TestTransitions();
	}

	internal void Leave( bool dispatch )
	{
		if ( dispatch )
		{
			OnLeaveState?.Invoke();
			GameObject.Dispatch( new LeaveStateEvent( this ) );

			try
			{
				_nextTransition?.Action?.Invoke();
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}
		}

		Enabled = false;

		_nextTransition = null;
	}

	[ThreadStatic]
	private static List<TransitionComponent>? TestTransitions_Active;

	private void TestTransitions()
	{
		_nextTransition = null;

		TestTransitions_Active ??= new List<TransitionComponent>();
		TestTransitions_Active.Clear();

		TestTransitions_Active.AddRange( Components.GetAll<TransitionComponent>( FindMode.EnabledInSelf ) );
		TestTransitions_Active.Sort();

		foreach ( var transition in TestTransitions_Active )
		{
			try
			{
				if ( transition.Condition?.Invoke() is false ) continue;
			}
			catch ( Exception e )
			{
				Log.Error( e );
			}

			_nextTransition = transition;

			StateMachine.Transition( transition.Target );
			break;
		}

		TestTransitions_Active.Clear();
	}

	internal IReadOnlyList<StateComponent> GetAncestors()
	{
		var list = new List<StateComponent>();

		var parent = Parent;

		while ( parent != null )
		{
			list.Add( parent );
			parent = parent.Parent;
		}

		list.Reverse();

		return list;
	}
}

/// <summary>
/// Event dispatched on the owner when a <see cref="StateMachineComponent"/> changes state.
/// Only invoked on components on the same object as the new state.
/// </summary>
public record EnterStateEvent( StateComponent State ) : IGameEvent;

/// <inheritdoc cref="EnterStateEvent"/>
[Title( "Enter State Event" ), Group( "State Machines" ), Icon( "electric_bolt" )]
public sealed class EnterStateEventComponent : GameEventComponent<EnterStateEvent> { }

/// <summary>
/// Event dispatched on the owner when a <see cref="StateMachineComponent"/> changes state.
/// Only invoked on components on the same object as the old state.
/// </summary>
public record LeaveStateEvent( StateComponent State ) : IGameEvent;

/// <inheritdoc cref="LeaveStateEvent"/>
[Title( "Leave State Event" ), Group( "State Machines" ), Icon( "electric_bolt" )]
public sealed class LeaveStateEventComponent : GameEventComponent<LeaveStateEvent> { }

/// <summary>
/// Event dispatched on the owner every fixed update while a <see cref="StateComponent"/> is active.
/// Only invoked on components on the same object as the state.
/// </summary>
public record UpdateStateEvent( StateComponent State ) : IGameEvent;

/// <inheritdoc cref="UpdateStateEvent"/>
[Title( "Update State Event" ), Group( "State Machines" ), Icon( "electric_bolt" )]
public sealed class UpdateStateEventComponent : GameEventComponent<UpdateStateEvent> { }
