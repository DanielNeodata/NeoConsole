using Microsoft.CodeAnalysis.CSharp.Scripting;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NeoConsole
{
	public static class Interpreter
	{
		public static async Task Invoke(Context _CTX)
		{
			try
			{
				Tools.ConsoleClear();
				Tools.PrepareContext(_CTX);
				if (_CTX.State.Method != null)
				{
					_CTX.State.ReturnValue = _CTX.State.Method.Invoke(_CTX.Commands, _CTX.State.Arguments);
					await Response(_CTX);
				}
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}

		public static async Task RunAsync(Context _CTX)
		{
			try
			{
				Tools.ConsoleClear();
				Tools.PrepareContext(_CTX);
				_CTX.Status = await CSharpScript.RunAsync((_CTX.State.CodeVerified.ToString()), _CTX.Options, globals: _CTX.Commands);
				_CTX.State.ReturnValue = _CTX.Status.ReturnValue;
				await Response(_CTX);
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}

		public static async Task PosProccess(Context _CTX)
		{
			try
			{
				if (_CTX.State.ReturnValue!=null && _CTX.State.ReturnValue.ToString().StartsWith("do:"))
				{
					switch (_CTX.State.ReturnValue.ToString().Split(':')[1])
					{
						case "clear":
							Tools.ConsoleClear();
							break;
						case "help":
							Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);
							break;
						case "exit":
							System.Environment.Exit(0);
							break;
					}
				}
				Tools.ConsolePrompt(_CTX);
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}

		public static async Task Response(Context _CTX)
		{
			try
			{
				if (_CTX.State.ReturnValue != null && (!_CTX.State.ReturnValue.ToString().StartsWith("do:"))) {
					Tools.ConsoleWrite(_CTX, $"=> {_CTX.State.ReturnValue}", false, ConsoleColor.Green);
				}
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}
	}
}
