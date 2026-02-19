using System.Reflection;
using System.Text.RegularExpressions;

public static class PreProcesador
{
    public static string RepararTipos(string codigoUsuario, object instanciaGlobals)
    {
        // 1. Obtenemos todos los métodos de la clase Comandos mediante Reflection
        var metodos = instanciaGlobals.GetType()
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var metodo in metodos)
        {
            // Buscamos si el nombre del método aparece en el código del usuario
            // Ejemplo: Saludar(123)
            string patron = $@"\b{metodo.Name}\s*\((.*?)\)";
            var coincidencias = Regex.Matches(codigoUsuario, patron);

            foreach (Match m in coincidencias)
            {
                string argumentosOriginales = m.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(argumentosOriginales)) continue;

                var parametrosInfo = metodo.GetParameters();
                var argumentosDivididos = argumentosOriginales.Split(',');

                if (argumentosDivididos.Length == parametrosInfo.Length)
                {
                    for (int i = 0; i < parametrosInfo.Length; i++)
                    {
                        var tipoRequerido = parametrosInfo[i].ParameterType;
                        var valorEntregado = argumentosDivididos[i].Trim();

                        // Si el método espera string pero el usuario NO puso comillas
                        if (tipoRequerido == typeof(string) && !valorEntregado.StartsWith("\""))
                        {
                            // Envolvemos el valor en un .ToString() para que Roslyn no proteste
                            argumentosDivididos[i] = $"({valorEntregado}).ToString()";
                        }
                    }

                    // Reconstruimos la llamada al método reparada
                    string nuevaLlamada = $"{metodo.Name}({string.Join(", ", argumentosDivididos)})";
                    codigoUsuario = codigoUsuario.Replace(m.Value, nuevaLlamada);
                }
            }
        }
        return codigoUsuario;
    }
}