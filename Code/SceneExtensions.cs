namespace Sandbox.Events;

public static class SceneExtensions
{
	/// <summary>
	/// Notifies all <see cref="IGameEventHandler{T}"/> components that are within <paramref name="sender"/>,
	/// with a payload of type <typeparamref name="T"/>.
	/// </summary>
	public static void DispatchGameEvent<T>( this GameObject sender, T eventArgs )
	{
		GameEvent<T>.Dispatch( sender, eventArgs );
	}
}
