using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace NeoConsole
{

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Funciones comandosRef = new Funciones();
            MethodInfo[] metodos = typeof(Funciones).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            var opciones = ScriptOptions.Default
                .AddReferences(typeof(Funciones).Assembly)
                .AddImports("System", "System.Linq", "System.Collections.Generic", "System.Math");

            ScriptState<object> estado = null;
            StringBuilder bufferCode = new StringBuilder();
            int nivelIndentacion = 0;

            Console.WriteLine("=== C# AVANZADO (Multilínea) ===");
            Console.WriteLine("Si escribes ';' o '}' al final de la linea, ejecuta la sentencia y la mantiene en el Buffer");
            Console.WriteLine("Si escribes la sentencia sin ';' al final ejecuta el método que esté disponible o da error.");
            MostrarAyuda(metodos);

            while (true)
            {
                // Cambiamos el prompt si estamos dentro de un bloque
                Console.ForegroundColor = nivelIndentacion > 0 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
                Console.Write(nivelIndentacion > 0 ? "... " : "C#> ");
                Console.ResetColor();

                string entrada = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(entrada) && bufferCode.Length == 0)
                {
                    MostrarAyuda(metodos);
                    ListarMetodosDinamicos(estado);
                    continue;
                }
                if (entrada?.ToLower() == "salir" || entrada?.ToLower() == "exit" || entrada?.ToLower() == "quit") break;
                if (entrada?.ToLower() == "cls") { Console.Clear(); MostrarAyuda(metodos); continue; }
                if (entrada?.ToLower() == "clear") { Console.Clear(); bufferCode.Clear(); nivelIndentacion = 0; estado = null; MostrarAyuda(metodos); continue; }

                bufferCode.AppendLine(entrada);

                // Contamos llaves para saber si el bloque está completo
                nivelIndentacion += entrada.Count(f => f == '{') - entrada.Count(f => f == '}');

                // Si no hay bloques abiertos, ejecutamos
                if (nivelIndentacion <= 0)
                {
                    string codigoAEjecutar = bufferCode.ToString();
                    bufferCode.Clear();
                    nivelIndentacion = 0; // Reset por seguridad

                    // Forzamos los tipos de variables antes de compilar
                    string codigoReparado = PreProcesador.RepararTipos(codigoAEjecutar, comandosRef);

                    bool existe = false;
                    try
                    {
                        string codigoVerificar = codigoReparado.Replace("(", " ");
                        codigoVerificar = codigoVerificar.Replace(")", " ");
                        codigoVerificar = codigoVerificar.Replace("\r", "");
                        codigoVerificar = codigoVerificar.Replace("\n", "");

                        // Regex mágica para separar por espacios pero respetar lo que está entre comillas
                        List<string> partes = Regex.Matches(codigoVerificar, @"[\""].+?[\""]|[^ ]+")
                            .Cast<Match>()
                            .Select(m => m.Value.Replace("\"", "")) // Quitamos las comillas al final
                            .ToList();
                        string nombreComando = partes[0];
                        string[] argumentos = partes.Skip(1).ToArray();

                        MethodInfo metodo = metodos.FirstOrDefault(m => m.Name.Equals(nombreComando, StringComparison.OrdinalIgnoreCase));

                        if (codigoVerificar[codigoVerificar.Length - 1].ToString() != ";" && codigoVerificar[codigoVerificar.Length - 1].ToString() != "}")
                        {
                            if (metodo == null)
                            {
                                if (estado != null)
                                {
                                    // Recorremos todos los scripts en la cadena (del más nuevo al más viejo)
                                    var scriptActual = estado.Script;
                                    var metodosVistos = new HashSet<string>(); // Para evitar duplicados si se redefine algo

                                    while (scriptActual != null)
                                    {
                                        var compilacion = scriptActual.GetCompilation();

                                        // Buscamos los símbolos en esta sumisión específica
                                        var simbolos = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member)
                                                                  .OfType<IMethodSymbol>()
                                                                  .Where(m => !m.IsImplicitlyDeclared &&
                                                                              m.MethodKind == MethodKind.Ordinary);

                                        foreach (var s in simbolos)
                                        {
                                            string firma = s.ToDisplayString();
                                            if (metodosVistos.Add(firma)) // Si es nuevo en la lista, lo imprimimos
                                            {
                                                if (s.Name == nombreComando)
                                                {
                                                    existe = true;
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
                            existe = true;
                        }

                        if (existe)
                        {
                            if (estado == null)
                            {
                                estado = await CSharpScript.RunAsync(codigoReparado, opciones, globals: comandosRef);
                            }
                            else
                            {
                                estado = await estado.ContinueWithAsync(codigoReparado);
                            }

                            if (estado.ReturnValue != null)
                            {
                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.WriteLine($"=> {estado.ReturnValue}");
                                Console.ResetColor();
                            }
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Comando no encontrado.");
                            Console.ResetColor();
                            MostrarAyuda(metodos);
                            ListarMetodosDinamicos(estado);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (existe)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"[Error]: {ex.Message}");
                            Console.ResetColor();
                        }
                    }

                }
            }

        }
        static void MostrarAyuda(MethodInfo[] methods)
        {
            Console.WriteLine("\n--- COMANDOS DISPONIBLES ---");
            foreach (var x in methods)
            {
                var paramsStr = string.Join(", ", x.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                Console.WriteLine($"  {x.Name.PadRight(15)} -> ({string.Join(", ", paramsStr)})");
            }
            Console.WriteLine("  Cls             -> Borra la pantalla");
            Console.WriteLine("  Clear           -> Borra la pantalla y el buffer");
            Console.WriteLine("  Salir           -> Cierra la aplicación");
            Console.WriteLine("----------------------------");
        }
        
        static void ListarMetodosDinamicos(ScriptState<object> estado)
        {
            if (estado == null) return;

            // Recorremos todos los scripts en la cadena (del más nuevo al más viejo)
            var scriptActual = estado.Script;
            var metodosVistos = new HashSet<string>(); // Para evitar duplicados si se redefine algo
            while (scriptActual != null)
            {
                Compilation compilacion = scriptActual.GetCompilation();

                // Buscamos los símbolos en esta sumisión específica
                var simbolos = compilacion.GetSymbolsWithName(s => true, SymbolFilter.Member)
                                          .OfType<IMethodSymbol>()
                                          .Where(m => !m.IsImplicitlyDeclared &&
                                                       m.MethodKind == MethodKind.Ordinary);

                foreach (var s in simbolos)
                {
                    string firma = s.ToDisplayString();
                    if (metodosVistos.Add(firma)) // Si es nuevo en la lista, lo imprimimos
                    {
                        Console.Write($"  {s.Name.ToString().PadRight(15)} -> (");
                        for (int qp = 0; qp < s.Parameters.Length; qp++)
                        {
                            Console.Write($"{s.Parameters[qp]}");
                        }
                        Console.WriteLine(")");
                    }
                }

                // Subimos al script anterior en la cadena de ContinueWith
                scriptActual = scriptActual.Previous;
            }
        }
    }
}