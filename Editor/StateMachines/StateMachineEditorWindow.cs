using Editor;
using System.Collections.Generic;
using System.Linq;

namespace Sandbox.Events.Editor;

public class StateMachineEditorWindow : DockWindow
{
	internal static List<StateMachineEditorWindow> AllWindows { get; } = new List<StateMachineEditorWindow>();
	private List<StateMachineView> Views { get; } = new();

	public StateMachineEditorWindow()
	{
		DeleteOnClose = true;

		Size = new Vector2( 1280, 720 );

		// Make this window stay on top of the editor, by making it a dialog
		Parent = EditorWindow;
		WindowFlags = WindowFlags.Dialog | WindowFlags.Customized | WindowFlags.CloseButton | WindowFlags.WindowSystemMenuHint | WindowFlags.WindowTitle | WindowFlags.MaximizeButton;

		WindowTitle = "State Machine Editor";

		SetWindowIcon( "smart_toy" );

		AllWindows.Add( this );

		RebuildUI();
	}

	public StateMachineView Open( StateMachineComponent stateMachine )
	{
		var view = new StateMachineView( stateMachine, this );

		var sibling = Views.LastOrDefault();

		Views.Add( view );

		if ( sibling is null )
		{
			DockManager.AddDock( null, view, DockArea.RightOuter, split: 1f );
		}
		else
		{
			DockManager.AddDock( sibling, view, DockArea.Inside );
		}

		DockManager.Update();

		return view;
	}

	protected override void OnClosed()
	{
		base.OnClosed();

		AllWindows.Remove( this );
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		// Move this window to the end of the list, so it has priority
		// when opening a new graph

		AllWindows.Remove( this );
		AllWindows.Add( this );
	}

	public void RebuildUI()
	{

	}

	[Shortcut( "editor.save", "CTRL+S", ShortcutType.Window )]
	public void Save()
	{
		var active = Views.FirstOrDefault( x => x is { IsValid: true, Visible: true } );

		active?.StateMachine.Scene.Editor.Save( false );
	}
}

