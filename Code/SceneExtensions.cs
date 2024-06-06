using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sandbox.Events;

public static class SceneExtensions
{
	public static void DispatchGameEvent<T>( this GameObject go, T eventArgs )
	{
		GameEvent<T>.Dispatch( go, eventArgs );
	}
}
