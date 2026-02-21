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

			/*Muestra prompt por default*/
			Tools.ConsolePrompt(_CTX);

			while (true)
			{
				_CTX.Input = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(_CTX.Input))
				{
					string _preCommand = "[run]";
					if (_CTX.Input.ToLower().StartsWith(_preCommand))
					{
						_CTX.Input = _CTX.Input.Substring(_preCommand.Length);
						/*Mecanismo para agregar method a la clase instanciada?*/
						await Interpreter.RunAsync(_CTX);
					}
					else
					{
						/*Llamada asumiendo se invoca un método definido en la clase del _CTX o uno de los comando de posprocesamiento de Abstract*/
						await Interpreter.Invoke(_CTX);
					}

					/*Posprocesamiento de valores de respuesta*/
					await Interpreter.PosProccess(_CTX);
				}
				else
				{
					Tools.ConsoleWrite(_CTX, "No se ha enviado ningún comando", true, ConsoleColor.Yellow);
					Tools.ConsolePrompt(_CTX);
				}
			}
		}
	}
}