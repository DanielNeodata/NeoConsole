using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole.Classes
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
				}
				else
				{
					if (_CTX.Status != null)
					{
						Script scriptActual = _CTX.Status.Script;
						while (scriptActual != null)
						{
							Compilation compilation = scriptActual.GetCompilation();
							IEnumerable<ISymbol> symbols = compilation.GetSymbolsWithName(s => true, SymbolFilter.Member).OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);
							foreach (ISymbol s in symbols)
							{
								if (s.Name == _CTX.State.CommandName)
								{
									_CTX.Status = await _CTX.Status.ContinueWithAsync(_CTX.State.CodeVerified.ToString());
									_CTX.State.ReturnValue = _CTX.Status.ReturnValue;
									break;
								}
							}
							scriptActual = scriptActual.Previous;
						}
					}
					else
					{
						throw new Exception("Imposible ejecutar el comando solicitado");
					}
				}
				await Response(_CTX);
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
				if (_CTX.State.ReturnValue != null && _CTX.State.ReturnValue.ToString().StartsWith("do:"))
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
				if (_CTX.State.ReturnValue != null && (!_CTX.State.ReturnValue.ToString().StartsWith("do:")))
				{
					Tools.ConsoleReturnValue(_CTX);
				}
			}
			catch (Exception ex)
			{
				Tools.ConsoleError(_CTX, ex);
			}
		}
	}
}
