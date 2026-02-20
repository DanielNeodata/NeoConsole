using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole
{
	public static class Interpreter
	{
		public static async Task Evaluate(Context _CTX)
		{
			State _State = Tools.FixTypes(_CTX);
			try
			{
				switch (_State.LastChar)
				{
					case ";":
					case "}":
						_State.Exists = true;
						break;
					default:
						if (_State.Method == null && _CTX.Status != null)
						{
							/*Recorremos todos los scripts en la cadena (del más nuevo al más viejo)*/
							Script scriptActual = _CTX.Status.Script;
							HashSet<string> methodViewed = new HashSet<string>(); // Para evitar duplicados si se redefine algo

							while (scriptActual != null)
							{
								/*Buscamos los símbolos en esta sumisión específica*/
								IEnumerable<ISymbol> symbols = scriptActual.GetCompilation().GetSymbolsWithName(s => true, SymbolFilter.Member)
														  .OfType<IMethodSymbol>()
														  .Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);

								foreach (ISymbol s in symbols)
								{
									/*Si es nuevo en la lista, lo imprimimos*/
									if (methodViewed.Add(s.ToDisplayString()))
									{
										if (s.Name == _State.CommandName) { _State.Exists = true; break; }
									}
								}
								/*Subimos al script anterior en la cadena de ContinueWith*/
								scriptActual = scriptActual.Previous;
							}
						}
						break;
				}

				if (_State.Exists)
				{
					if (_CTX.Status == null)
					{
						_CTX.Status = await CSharpScript.RunAsync(_State.CodeVerified.ToString(), _CTX.Options, globals: _CTX.Commands);
					}
					else
					{
						_CTX.Status = await _CTX.Status.ContinueWithAsync(_State.CodeVerified.ToString());
					}

					if (_CTX.Status.ReturnValue != null) { Tools.ConsoleWrite($"=> {_CTX.Status.ReturnValue}", false, ConsoleColor.Green); }
				}
				else
				{
					Tools.ConsoleWrite($"Comando '{_State.CommandName}' no encontrado.", true, ConsoleColor.Red);
					Tools.ListDynMethods(_CTX);
				}
			}
			catch (Exception ex)
			{
				if (_State.Exists) { Tools.ConsoleWrite($"[Error]: {ex.Message}", true, ConsoleColor.Red); }
			}
		}
	}
}
