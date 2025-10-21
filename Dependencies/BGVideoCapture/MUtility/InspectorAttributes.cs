using UnityEngine;
using System.Collections;


public class InspectorButton : System.Attribute
{
	public float spaceBefore = 0f;

	public InspectorButton (float spaceBefore)
	{
		this.spaceBefore = spaceBefore;
	}

	public InspectorButton ()
	{
	}
}

public class InspectorHelpSymbol : System.Attribute
{
	public string message;
	public InspectorHelpSymbol (string msg)
	{
		this.message = msg;
	}

}