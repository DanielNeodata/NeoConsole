using NeoConsole.Classes;

namespace NeoConsole.BaseClasses
{
	public class Abstract
	{
		[CustomDescription("Cambia el contexto")]
		public string Change(string _context)
		{
			return $"do:change:{_context}";
		}
		[CustomDescription("Limpia la pantalla")]
		public string Clear()
		{
			return "do:clear";
		}
		[CustomDescription("Muestra ayuda completa")]
		public string Help()
		{
			return "do:help";
		}
		[CustomDescription("Cierra la consola")]
		public string Exit()
		{
			return "do:exit";
		}
	}
}
