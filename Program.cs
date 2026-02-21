using System;
using System.Linq;
using System.Threading.Tasks;

namespace NeoConsole
{

	internal class Program
	{
		static async Task Main(string[] args)
		{
			/*Instancia clase de control*/
			Context _CTX = new Context("Test", new Test());

			/*Muestra ayuda por default*/
			Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);

			while (true)
			{
				_CTX.Input = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(_CTX.Input))
				{
					if (_CTX.Input.ToLower().StartsWith("[add_method]"))
					{
						/*Mecanismo para agregar method a la clase instanciada?*/
					}
					else
					{
						await Interpreter.Evaluate(_CTX);
						/*Posprocesamiento de valores de respuesta*/
						if (_CTX.Status.ReturnValue.ToString().StartsWith("do:")) {
							switch (_CTX.Status.ReturnValue.ToString().Split(':')[1]) {
								case "clear":
									Tools.ConsoleClear();
									Tools.ConsolePrompt(_CTX);
									break;
								case "help":
									Tools.ConsoleWrite(_CTX, Tools.Help(_CTX).ToString(), true, null);
									break;
								case "exit":
									System.Environment.Exit(0);
									break;
							}
						}
					}
				}
				else
				{
					Tools.ConsoleWrite(_CTX, "No se ha enviado ningún comando", true, ConsoleColor.Magenta);
				}
			}
		}
	}
}