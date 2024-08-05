using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Sandbox.Events;

[Title( "Random Transition"), Icon( "casino" )]
public sealed class RandomTransition : Component, ITransitionSource,
	IGameEventHandler<EnterStateEvent>
{
	public class Entry : ITransition
	{
		/// <summary>
		/// State to transition to.
		/// </summary>
		[KeyProperty]
		public StateComponent? State { get; set; }

		/// <summary>
		/// Relative chance of this transition being chosen.
		/// </summary>
		[KeyProperty]
		public float Weight { get; set; } = 1f;

		/// <summary>
		/// Optional delay before changing state if this transition is chosen.
		/// </summary>
		public float DelaySeconds { get; set; }

		/// <summary>
		/// Optional predicate to decide if this transition is enabled.
		/// </summary>
		public Func<bool>? Condition { get; set; }

		[JsonIgnore]
		internal bool IsEnabled { get; set; }

		StateComponent ITransition.TargetState => State!;
	}

	[Property]
	public List<Entry> Entries { get; set; } = new();

	void IGameEventHandler<EnterStateEvent>.OnGameEvent( EnterStateEvent eventArgs )
	{
		if ( Entries is not { Count: > 0 } ) return;

		var totalWeight = 0f;

		foreach ( var entry in Entries )
		{
			entry.IsEnabled = entry.State is not null && entry.Weight > 0f && entry.Condition?.Invoke() is not false;

			if ( entry.IsEnabled )
			{
				totalWeight += entry.Weight;
			}
		}

		if ( totalWeight <= 0f ) return;

		var random = Random.Shared.NextSingle() * totalWeight;

		foreach ( var entry in Entries )
		{
			if ( !entry.IsEnabled ) continue;

			random -= entry.Weight;

			if ( random <= 0f )
			{
				eventArgs.State.StateMachine.Transition( entry.State!, entry.DelaySeconds );
				break;
			}
		}
	}

	IEnumerable<ITransition> ITransitionSource.Transitions => Entries.Where( x => x.State is not null );
}
