using Facepunch.ActionGraphs;
using System;

namespace Sandbox.Events.Editor;

partial class TransitionItem
{
	private Func<bool> CreateDelayGraph( float seconds )
	{
		var json =
			$$"""
			{
			  "__version": 7,
			  "__guid": "{{Guid.NewGuid()}}",
			  "UserData": {
			    "Title": "{{FormatDuration( seconds )}}",
			    "Icon": "timer",
			    "ReferencedComponentTypes": [
			      "Sandbox.Events.StateComponent"
			    ]
			  },
			  "Variables": [],
			  "Nodes": [
			    {
			      "Id": 0,
			      "Type": "input"
			    },
			    {
			      "Id": 1,
			      "Type": "output",
			      "UserData": {
			        "Position": "416,-0"
			      }
			    },
			    {
			      "Id": 2,
			      "Type": "scene.get",
			      "Properties": {
			        "T": "Sandbox.Events.StateComponent"
			      },
			      "UserData": {
			        "Position": "-0,48"
			      }
			    },
			    {
			      "Id": 3,
			      "Type": "property",
			      "ParentId": 2,
			      "Properties": {
			        "_type": "Sandbox.Events.StateComponent",
			        "_name": "Time"
			      }
			    },
			    {
			      "Id": 4,
			      "Type": "op.greaterthanorequal",
			      "UserData": {
			        "Position": "224,144"
			      }
			    }
			  ],
			  "Links": [
			    {
			      "SrcId": 4,
			      "SrcName": "_result",
			      "DstId": 1,
			      "DstName": "_result"
			    },
			    {
			      "SrcId": 0,
			      "SrcName": "_signal",
			      "DstId": 1,
			      "DstName": "_signal"
			    },
			    {
			      "SrcId": 0,
			      "SrcName": "_target",
			      "DstId": 2,
			      "DstName": "_this"
			    },
			    {
			      "SrcId": 2,
			      "SrcName": "_result",
			      "DstId": 3,
			      "DstName": "_target"
			    },
			    {
			      "SrcId": 3,
			      "SrcName": "_result",
			      "DstId": 4,
			      "DstName": "a"
			    },
			    {
			      "Value": {
			        "$type": "Simple",
			        "Type": "System.Single",
			        "Value": {{seconds:R}}
			      },
			      "DstId": 4,
			      "DstName": "b"
			    }
			  ]
			}
			""";

		using var sceneScope = Transition!.Scene.Push();
		using var targetScope = ActionGraph.PushTarget( InputDefinition.Target( typeof(GameObject), Transition.GameObject ) );

		return Json.Deserialize<Func<bool>>( json );
	}
}
