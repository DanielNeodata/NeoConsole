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

		public static async Task<string> EvalInput(Context _CTX)
		{
			string _preCommand = "";
			/*Verifica que el input tenga valor significativo*/
			if (string.IsNullOrWhiteSpace(_CTX.Input)) { throw new Exception("No se ha enviado ningún comando"); }

			/*Evalua sin el input tiene prefijo válido y ejecuta de acuerdo a la lógica provista*/
			foreach (KeyValuePair<string, Info> entry in _CTX.Prefixs)
			{
				if (_CTX.Input.ToLower().StartsWith(entry.Value.Key))
				{
					_preCommand = entry.Value.Key;
					break;
				}
			}

			/*Si no hay un prefijo válido, sale para ejecutar el Invoke*/
			if (_preCommand != "")
			{

				/*Quita el prefijo*/
				_CTX.Input = _CTX.Input.Substring(_preCommand.Length);

				/*Selecciona acción de acuerdo al precommand enviado*/
				switch (_preCommand)
				{
					case "[run]":
						/*Ejecucion de comando no definido en Abstract, _CTX, ni en los scripts del State del _CTX */
						await Interpreter.RunAsync(_CTX);
						break;
					default:
						_preCommand = "";
						break;
				}
			}
			if (_preCommand == "")
			{
				Tools.ConsoleWrite(_CTX, "El prefijo de acción enviado, no tiene funcionalidad definida", true, ConsoleColor.Yellow);
				Tools.ConsolePrompt(_CTX);
			}

			return _preCommand;
		}
	}
}
