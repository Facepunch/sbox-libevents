using System.Linq;
using Editor;
using Sandbox.UI;
using Button = Editor.Button;

namespace Sandbox.Events.Editor;

[CustomEditor( typeof(StateMachineComponent)) ]
public sealed class StateMachineComponentWidget : ComponentEditorWidget
{
	public StateMachineComponentWidget( SerializedObject obj ) : base( obj )
	{
		Layout = Layout.Column();
		Layout.Margin = new Margin( 30f, 20f );

		RebuildUI();
	}

	public void RebuildUI()
	{
		Layout.Add( new Button( "Open in Editor", "edit" )
		{
			Clicked = OnOpenEditor
		} );
	}

	private void OnOpenEditor()
	{
		StateMachineView.Open( SerializedObject.Targets.OfType<StateMachineComponent>().First() );
	}
}
