using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Threading.Tasks;

namespace NeoConsole
{
	public static class Interpreter
	{
		public static async Task Evaluate(Context _CTX)
		{
			try
			{
				Tools.PrepareContext(_CTX);
				_CTX.Status = await CSharpScript.RunAsync(_CTX.State.CodeVerified.ToString(), _CTX.Options, globals: _CTX.Commands);
				if (_CTX.Status.ReturnValue != null) {
					if (!_CTX.Status.ReturnValue.ToString().StartsWith("do:")){
						Tools.ConsoleWrite(_CTX, $"=> {_CTX.Status.ReturnValue}", false, ConsoleColor.Green);
					}
				}
			}
			catch (Exception ex)
			{
				Tools.ConsoleWrite(_CTX, $"[Error]: {ex.Message}", true, ConsoleColor.Red);
			}
		}
	}
}
