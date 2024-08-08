using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Diagnostics;

namespace Sandbox.Events;

/// <summary>
/// <para>
/// A state machine containing a set of <see cref="StateComponent"/>s. The <see cref="GameObject"/> containing
/// the currently active state will be enabled (including its ancestors), and all other objects containing states
/// are disabled.
/// </para>
/// <para>
/// The currently active state is controlled by the owner, and synchronised over the network. When a transition occurs,
/// a <see cref="LeaveStateEvent"/> is dispatched on the old state's containing object, followed by a
/// <see cref="EnterStateEvent"/> event on the object containing the new state. These events are only dispatched
/// on the owner.
/// </para>
/// </summary>
[Title( "State Machine" ), Icon( "smart_toy" ), Category( "State Machines" )]
public sealed class StateMachineComponent : Component
{
	private StateComponent? _currentState;

	/// <summary>
	/// How many instant state transitions in a row until we throw an error?
	/// </summary>
	public const int MaxInstantTransitions = 16;

	/// <summary>
	/// Which state is currently active?
	/// </summary>
	[Property, Sync]
	public StateComponent? CurrentState
	{
		get => _currentState;
		set
		{
			if ( _currentState == value ) return;
			_currentState = value;

			if ( Network.IsProxy )
			{
				EnableActiveStates( false );
			}
		}
	}

	[Property, Sync]
	public StateComponent? InitialState { get; set; }

	[Sync]
	public StateComponent? NextState { get; set; }

	/// <summary>
	/// All states found on descendant objects.
	/// </summary>
	public IEnumerable<StateComponent> States => Components.GetAll<StateComponent>( FindMode.EverythingInSelfAndDescendants );

	protected override void OnStart()
	{
		foreach ( var state in States )
		{
			state.Enabled = false;
			state.GameObject.Enabled = state.GameObject == GameObject;
		}

		if ( !Network.IsProxy && InitialState is { } initial )
		{
			CurrentState = initial;
			Transition( initial );
		}
	}

	private void EnableActiveStates( bool dispatch )
	{
		var current = CurrentState;
		var active = current?.GetAncestors() ?? Array.Empty<StateComponent>();
		var activeSet = active.ToHashSet();

		var toDeactivate = new Queue<StateComponent>( States.Where( x => x.Enabled && !activeSet.Contains( x ) ).Reverse() );
		var toActivate = new Queue<StateComponent>( active.Where( x => !x.Enabled ) );

		if ( current != null )
		{
			toActivate.Enqueue( current );
		}

		while ( toDeactivate.TryDequeue( out var next ) )
		{
			next.Leave( dispatch );

			if ( toDeactivate.All( x => x.GameObject != next.GameObject ) && toActivate.All( x => x.GameObject != next.GameObject ) )
			{
				next.GameObject.Enabled = false;
			}
		}

		while ( toActivate.TryDequeue( out var next ) )
		{
			next.GameObject.Enabled = true;

			next.Enter( dispatch );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( Network.IsProxy )
		{
			return;
		}

		if ( CurrentState is not { } current ) return;

		current.Update();

		var transitions = 0;

		while ( transitions++ < MaxInstantTransitions )
		{
			if ( NextState is not { } next )
			{
				return;
			}

			ClearTransition();
			CurrentState = next;
			EnableActiveStates( true );
		}
	}

	/// <summary>
	/// Queue up a transition to the given state. This will occur at the end of
	/// a fixed update on the state machine.
	/// </summary>
	public void Transition( StateComponent next )
	{
		Assert.NotNull( next );
		Assert.False( Network.IsProxy );

		NextState = next;
	}

	/// <summary>
	/// Removes any pending transitions, so this state machine will remain in the
	/// current state until another transition is queued with <see cref="Transition"/>.
	/// </summary>
	public void ClearTransition()
	{
		Assert.False( Network.IsProxy );

		NextState = null;
	}
}
