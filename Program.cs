using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NeoConsole
{

    internal class Program
    {
        static async Task Main(string[] args)
        {
            /*Define opciones*/
            ScriptOptions _opt = ScriptOptions.Default
                .AddReferences(typeof(Funciones).Assembly)
                .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Math");
            
            /*Instancia clase de control*/
            State _S = new State("uno", _opt);
            
            /*Muestra ayuda por default*/
			Console.Write(_S.Help().ToString());

            while (true)
            {
                // Cambiamos el prompt si estamos dentro de un bloque
                Console.ForegroundColor = _S.indent > 0 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
                Console.Write(_S.indent > 0 ? "... " : "C#> ");
                Console.ResetColor();

				/*-------------------------------------------------*/
				/*Tratamiento del comando enviado por el prompt*/
				/*-------------------------------------------------*/
				bool _continue = true;
				string input = Console.ReadLine();
                /*Executa el commando enviado por linea*/
				StringBuilder _sb = _S.Exec(input, out _continue);

                Console.Clear();
                Console.Write(_sb.ToString());
                if (!_continue) { break; }
				_S.bufferCode.AppendLine(input);

				// Contamos llaves para saber si el bloque está completo
				_S.indent += (input.Count(f => f == '{') - input.Count(f => f == '}'));

                // Si no hay bloques abiertos, ejecutamos
                if (_S.indent <= 0)
                {
					bool exists = false;
					string codeToExec = _S.bufferCode.ToString();
					_S.bufferCode.Clear();
					_S.indent = 0; // Reset por seguridad

                    // Forzamos los tipos de variables antes de compilar
                    string codeFixed = PreProcesador.RepararTipos(codeToExec, _S.Commands);
                    try
                    {
                        string codeVerified = codeFixed.Replace("(", " ").Replace(")", " ").Replace("\r", "").Replace("\n", "");
						int iLen = (codeVerified.Length - 1);

						// Regex mágica para separar por espacios pero respetar lo que está entre comillas
						List<string> segments = Regex.Matches(codeVerified, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value.Replace("\"", "")) // Quitamos las comillas al final
                            .ToList();
                        string commandName = segments[0];
                        string[] arguments = segments.Skip(1).ToArray();

                        MethodInfo Method = _S.Methods.FirstOrDefault(m => m.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
                        if (codeVerified[iLen].ToString() != ";" && codeVerified[iLen].ToString() != "}")
                        {
                            if (Method == null)
                            {
                                if (_S.Status != null)
                                {
                                    // Recorremos todos los scripts en la cadena (del más nuevo al más viejo)
                                    Script scriptActual = _S.Status.Script;
									HashSet<string> methodViewed = new HashSet<string>(); // Para evitar duplicados si se redefine algo

                                    while (scriptActual != null)
                                    {
                                        Compilation compilacion = scriptActual.GetCompilation();

                                        // Buscamos los símbolos en esta sumisión específica
                                        IEnumerable<ISymbol> symbols = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member)
                                                                  .OfType<IMethodSymbol>()
                                                                  .Where(m => !m.IsImplicitlyDeclared &&
                                                                              m.MethodKind == MethodKind.Ordinary);

                                        foreach (ISymbol s in symbols)
                                        {
                                            string firma = s.ToDisplayString();
                                            if (methodViewed.Add(firma)) // Si es nuevo en la lista, lo imprimimos
                                            {
                                                if (s.Name == commandName)
                                                {
                                                    exists = true;
                                                    break;
                                                }
                                            }
                                        }

                                        // Subimos al script anterior en la cadena de ContinueWith
                                        scriptActual = scriptActual.Previous;
                                    }
                                }
                            }
                        }
                        else
                        {
                            exists = true;
                        }

                        if (exists)
                        {
                            if (_S.Status == null)
                            {
								_S.Status = await CSharpScript.RunAsync(codeFixed, _S.Options, globals: _S.Commands);
                            }
                            else
                            {
								_S.Status = await _S.Status.ContinueWithAsync(codeFixed);
                            }

                            if (_S.Status.ReturnValue != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine("---Respuesta:");
								Console.WriteLine($"=> {_S.Status.ReturnValue}");
								Console.WriteLine("----------------------------End.");
								Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Comando no encontrado.");
                            Console.ResetColor();
							_S.ListDynMethods();
						}
                    }
                    catch (Exception ex)
                    {
                        if (exists)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Error]: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                }
            }

        }
    }
}