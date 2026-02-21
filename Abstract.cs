using System;
using System.Diagnostics;
using System.Text;

namespace NeoConsole
{
	public class Abstract
	{
		public string Clear()
		{
			return "do:clear";
		}
		public string Help()
		{
			return "do:help";
		}
		public string Exit()
		{
			return "do:exit";
		}
	}
}
