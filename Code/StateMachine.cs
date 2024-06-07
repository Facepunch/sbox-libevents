using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Diagnostics;

namespace Sandbox.Events;

[Title( "State Machine" ), Category( "State Machines" )]
public sealed class StateMachineComponent : Component
{
	private Guid _currentStateGuid;

	[HostSync]
	private Guid CurrentStateGuid
	{
		get => _currentStateGuid;
		set
		{
			if ( _currentStateGuid == value ) return;
			_currentStateGuid = value;

			EnableActiveStates( Networking.IsHost );
		}
	}

	[HostSync]
	private Guid NextStateGuid { get; set; }

	[HostSync]
	public float NextStateTime { get; set; }

	[Property]
	public StateComponent? CurrentState
	{
		get => Scene.Directory.FindComponentByGuid( CurrentStateGuid ) as StateComponent;
		set => CurrentStateGuid = value?.Id ?? Guid.Empty;
	}

	public StateComponent? NextState
	{
		get => Scene.Directory.FindComponentByGuid( NextStateGuid ) as StateComponent;
		private set => NextStateGuid = value?.Id ?? Guid.Empty;
	}

	public IEnumerable<StateComponent> States => Components.GetAll<StateComponent>( FindMode.EverythingInSelfAndDescendants );

	protected override void OnStart()
	{
		foreach ( var state in States )
		{
			state.Enabled = false;
			state.GameObject.Enabled = state.GameObject == GameObject;
		}

		if ( Networking.IsHost && CurrentState is { } current )
		{
			Transition( current );
		}
	}

	private void EnableActiveStates( bool dispatch )
	{
		var active = CurrentState?.GetAncestorsIncludingSelf() ?? Array.Empty<StateComponent>();
		var activeSet = active.ToHashSet();

		var toDeactivate = new Queue<StateComponent>( States.Where( x => x.Enabled && !activeSet.Contains( x ) ).Reverse() );
		var toActivate = new Queue<StateComponent>( active.Where( x => !x.Enabled ) );

		while ( toDeactivate.TryDequeue( out var next ) )
		{
			Leave( next, dispatch );

			if ( toDeactivate.All( x => x.GameObject != next.GameObject ) && toActivate.All( x => x.GameObject != next.GameObject ) )
			{
				next.GameObject.Enabled = false;
			}
		}

		while ( toActivate.TryDequeue( out var next ) )
		{
			next.GameObject.Enabled = true;

			Enter( next, dispatch );
		}
	}

	private void Enter( StateComponent state, bool dispatch )
	{
		state.Enabled = true;

		if ( dispatch )
		{
			state.GameObject.DispatchGameEvent( new EnterStateEventArgs( state ) );
		}
	}

	private void Leave( StateComponent state, bool dispatch )
	{
		if ( dispatch )
		{
			state.GameObject.DispatchGameEvent( new LeaveStateEventArgs( state ) );
		}

		state.Enabled = false;
	}

	protected override void OnFixedUpdate()
	{
		if ( !Networking.IsHost )
		{
			return;
		}

		if ( CurrentState is not { } current )
		{
			return;
		}

		Scene.DispatchGameEvent( new UpdateStateEventArgs( current ) );

		if ( NextState is not { } next || !(Time.Now >= NextStateTime) )
		{
			return;
		}

		if ( next.DefaultNextState is not null )
		{
			Transition( next.DefaultNextState, next.DefaultDuration );
		}
		else
		{
			ClearTransition();
		}

		CurrentState = next;
	}

	/// <summary>
	/// Queue up a transition to the given state. This will occur at the end of
	/// a fixed update on the state machine.
	/// </summary>
	public void Transition( StateComponent next, float delaySeconds = 0f )
	{
		Assert.NotNull( next );
		Assert.True( Networking.IsHost );

		NextState = next;
		NextStateTime = Time.Now + delaySeconds;
	}

	public void ClearTransition()
	{
		Assert.True( Networking.IsHost );

		NextState = null;
		NextStateTime = float.PositiveInfinity;
	}
}
