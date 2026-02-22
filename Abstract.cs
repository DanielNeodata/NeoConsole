namespace NeoConsole
{
	public class Abstract
	{
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
