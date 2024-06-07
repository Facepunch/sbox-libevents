using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sandbox.Events;

public interface IGameEventHandler<in T>
{
	void OnGameEvent( GameObject sender, T eventArgs );
}

/// <summary>
/// Helper for dispatching game events in a scene.
/// </summary>
public static class GameEvent<T>
{
	// ReSharper disable once StaticMemberInGenericType
	private static IReadOnlyDictionary<Type, int>? HandlerOrdering { get; set; }

	/// <summary>
	/// Notifies all <see cref="IGameEventHandler{T}"/> components that are within <paramref name="sender"/>,
	/// with a payload of type <typeparamref name="T"/>.
	/// </summary>
	public static void Dispatch( GameObject sender, T eventArgs )
	{
		HandlerOrdering ??= GetHandlerOrdering();

		var handlers = sender is Scene scene
			? scene.GetAllComponents<IGameEventHandler<T>>() // I think this is more efficient?
			: sender.Components.GetAll<IGameEventHandler<T>>();

		foreach ( var handler in handlers.OrderBy( x => HandlerOrdering[x.GetType()] ) )
		{
			handler.OnGameEvent( sender, eventArgs );
		}
	}

	private static bool IsImplementingMethodName( string methodName )
	{
		if ( methodName == nameof(IGameEventHandler<T>.OnGameEvent) )
		{
			return true;
		}

		return methodName.StartsWith( "Sandbox.Events.IGameEventHandler<" ) && methodName.EndsWith( ">.OnGameEvent" );
	}

	private static MethodDescription? GetImplementation( TypeDescription type )
	{
		foreach ( var method in type.Methods )
		{
			if ( method.IsStatic ) continue;
			if ( method.Parameters.Length != 2 ) continue;
			if ( method.Parameters[0].ParameterType != typeof( GameObject ) ) continue;
			if ( method.Parameters[1].ParameterType != typeof( T ) ) continue;

			if ( !IsImplementingMethodName( method.Name ) ) continue;

			return method;
		}

		return null;
	}

	private static IReadOnlyDictionary<Type, int> GetHandlerOrdering()
	{
		var types = TypeLibrary.GetTypes<IGameEventHandler<T>>().ToArray();
		var helper = new SortingHelper( types.Length );

		for ( var i = 0; i < types.Length; ++i )
		{
			var type = types[i];
			var method = GetImplementation( type );

			if ( method is null )
			{
				Log.Warning( $"Can't find {nameof( IGameEventHandler<T> )}<{typeof( T ).Name}> implementation in {type.Name}!" );
				continue;
			}

			foreach ( var attrib in method.Attributes )
			{
				switch ( attrib )
				{
					case EarlyAttribute:
						helper.AddFirst( i );
						break;

					case LateAttribute:
						helper.AddLast( i );
						break;

					case IBeforeAttribute before:
						for ( var j = 0; j < types.Length; ++j )
						{
							if ( i == j ) continue;

							var other = types[j];

							if ( before.Type.IsAssignableFrom( other.TargetType ) )
							{
								helper.AddConstraint( i, j );
							}
						}

						break;

					case IAfterAttribute after:
						for ( var j = 0; j < types.Length; ++j )
						{
							if ( i == j ) continue;

							var other = types[j];

							if ( after.Type.IsAssignableFrom( other.TargetType ) )
							{
								helper.AddConstraint( j, i );
							}
						}

						break;
				}
			}
		}

		var ordering = new List<int>();

		if ( !helper.Sort( ordering, out var invalid ) )
		{
			Log.Error( $"Invalid event ordering constraint between {types[invalid.EarlierIndex].Name} and {types[invalid.LaterIndex].Name}!" );
			return ImmutableDictionary<Type, int>.Empty;
		}

		return Enumerable.Range( 0, ordering.Count )
			.ToImmutableDictionary( i => types[ordering[i]].TargetType, i => i );
	}
}
