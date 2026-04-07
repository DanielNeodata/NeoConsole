using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.ML;
using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace NeoConsole.Classes
{
    public static class Tools
    {
        public static void ConsoleClear()
        {
            Console.Clear();
        }
        public static void ConsolePrompt(Context _CTX)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(Environment.NewLine);
            Console.Write("ND#> ");
            Console.ResetColor();
        }
        public static void ConsoleWrite(Context _CTX, string? _data, bool _clear, ConsoleColor? _color)
        {
            if (_color == null) { _color = ConsoleColor.White; }
            Console.ForegroundColor = (ConsoleColor)_color;
            if (_clear) { Console.Clear(); }
            if (!string.IsNullOrWhiteSpace(_data)) { Console.WriteLine(_data); }
            Console.ResetColor();
        }
        public static void ConsoleReturnValue(Context _CTX)
        {
            ConsoleWrite(_CTX, Separator(), true, null);
            ConsoleWrite(_CTX, $"RESPUESTA", false, null);
            ConsoleWrite(_CTX, Separator(), false, null);
            ConsoleWrite(_CTX, $"{_CTX.State.ReturnValue}", false, ConsoleColor.Cyan);
            ConsoleWrite(_CTX, Separator(), false, null);
            ConsoleWrite(_CTX, $"ND# Timestamp: {DateTime.Now.ToString()}", false, null);
            ConsoleWrite(_CTX, Separator(), false, null);
        }
        public static void ConsoleError(Context _CTX, Exception ex)
        {
            ConsoleWrite(_CTX, $"[Error]: {ex.Message}", true, ConsoleColor.Red);
        }

        public static string Separator()
        {
            return string.Concat(Enumerable.Repeat("-", 100));
        }
        public static string ListMethods(MethodInfo[] _methods, string _title)
        {
            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine(_title);
            foreach (MethodInfo x in _methods)
            {
                string paramsStr = string.Join(", ", x.GetParameters().Select(p => $"{p.ParameterType.Name.ToLower()} {p.Name}"));
                CustomDescriptionAttribute attribute = (CustomDescriptionAttribute)Attribute.GetCustomAttribute(x, typeof(CustomDescriptionAttribute));
                string paramCustom = "";
                if (attribute != null) { paramCustom = $"-> ({attribute.Description})"; }
                _sb.AppendLine($"   {x.Name}({string.Join(", ", paramsStr)}) {paramCustom}");
            }
            return _sb.ToString();
        }
        public static StringBuilder Help(Context _CTX)
        {
            StringBuilder _sb = new StringBuilder();
            _sb.AppendLine(Separator());
            _sb.AppendLine("AYUDA");
            _sb.AppendLine(Separator());

            _sb.AppendLine("* Prefijos de acción:");
            foreach (KeyValuePair<string, Info> entry in _CTX.Prefixs) { _sb.AppendLine($"   {entry.Value.Key} -> ({entry.Value.Description})"); }
            _sb.AppendLine("");

            _sb.AppendLine("* Contextos disponibles:");
            foreach (KeyValuePair<string, Info> entry in _CTX.Contexts) { _sb.AppendLine($"   {entry.Value.Key} -> ({entry.Value.Description})"); }
            _sb.AppendLine("");

            _sb.AppendLine(ListMethods(_CTX.MethodsAbstract, "* Funciones abstractas:"));
            _sb.AppendLine(ListMethods(_CTX.Methods, "* Funciones definidas:"));

            if (_CTX.Status != null)
            {
                _sb.AppendLine("* Funciones dinámicas:");
                Script scriptActual = _CTX.Status.Script;
                while (scriptActual != null)
                {
                    Compilation compilation = scriptActual.GetCompilation();
                    IEnumerable<ISymbol> symbols = compilation.GetSymbolsWithName(s => true, SymbolFilter.Member).OfType<IMethodSymbol>().Where(m => !m.IsImplicitlyDeclared && m.MethodKind == MethodKind.Ordinary);
                    foreach (ISymbol s in symbols)
                    {
                        string _params = "";
                        foreach (var p in ((IMethodSymbol)s.OriginalDefinition).Parameters) { _params += ($"{p.ToString()}, "); }
                        char[] _t = { ',', ' ' };
                        _sb.AppendLine($"   {s.Name.ToString()}({_params.TrimEnd(_t)})");
                    }
                    scriptActual = scriptActual.Previous;
                }
            }
            _sb.AppendLine(Separator());
            _sb.AppendLine($"ND# Timestamp: {DateTime.Now.ToString()} Contexto activo: {_CTX.Key}");
            _sb.AppendLine(Separator());

            return _sb;
        }
        public static object ConvertStringToParameterType(string value, ParameterInfo parameterInfo)
        {
            Type targetType = parameterInfo.ParameterType;
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (value == null) { return null; }
                targetType = Nullable.GetUnderlyingType(targetType);
            }
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException)
            {
                return null;
            }
        }
        public static State PrepareContext(Context _CTX)
        {
            _CTX.State = new State();
            StringBuilder _sb = new StringBuilder();
            string codeToExec = _CTX.Input;
            _sb.Append(codeToExec);

            /*-------------------------------------------------------------------------------------------*/
            /*Asigna valores a la estructura de retorno*/
            /*-------------------------------------------------------------------------------------------*/
            string[] segments = codeToExec.Split('(');
            string[] arguments = Array.Empty<string>();

            if (segments.Length > 1) { arguments = segments[1].Replace(")", "").Trim().Split(','); }

            _CTX.State.CodeVerified = _sb;
            _CTX.State.LastChar = _sb[_sb.Length - 1].ToString();
            _CTX.State.CommandName = segments[0];
            _CTX.State.Method = _CTX.GetMethodByName(segments[0]);
            _CTX.State.Arguments = Array.Empty<object>();
            if (_CTX.State.Method != null && arguments.Length != 0)
            {
                ParameterInfo[] paramInfo = _CTX.State.Method.GetParameters();
                for (int i = 0; i < paramInfo.Length; i++)
                {
                    _CTX.State.Arguments = _CTX.State.Arguments.Append(ConvertStringToParameterType(arguments[i], paramInfo[i])).ToArray();
                }
            }
            /*-------------------------------------------------------------------------------------------*/

            return _CTX.State;
        }

        public static string ObtenerTopVariables(MLContext mlContext, ITransformer model, IDataView data)
        {
            try
            {
                var transformedData = model.Transform(data);
                var schema = transformedData.Schema;

                // 1. BUSCADOR INTELIGENTE: Si no se llama "Features", buscamos la columna vectorizada
                DataViewSchema.Column column = default;
                bool encontrada = false;

                foreach (var col in schema)
                {
                    // Buscamos la columna que sea un Vector de Single (float) y tenga SlotNames
                    if (col.Type is VectorDataViewType vectorType && vectorType.ItemType is NumberDataViewType)
                    {
                        column = col;
                        encontrada = true;
                        break;
                    }
                }

                if (!encontrada) return "No se detectó columna de características (Features)";

                // 2. EXTRAER NOMBRES (SLOT NAMES)
                VBuffer<ReadOnlyMemory<char>> slotNames = default;
                column.GetSlotNames(ref slotNames);
                var names = slotNames.DenseValues().Select(x => x.ToString()).ToArray();

                // 3. EXTRAER PESOS DEL ALGORITMO
                var chain = (TransformerChain<ITransformer>)model;
                var lastTransformer = chain.Last();
                var modelProperty = lastTransformer.GetType().GetProperty("Model");

                if (modelProperty != null)
                {
                    var modelParams = modelProperty.GetValue(lastTransformer);
                    var weightsProp = modelParams.GetType().GetProperty("Weights")
                                   ?? modelParams.GetType().GetProperty("FeatureWeights");

                    if (weightsProp != null)
                    {
                        var weights = (VBuffer<float>)weightsProp.GetValue(modelParams);
                        var weightsValues = weights.DenseValues().ToArray();

                        // 4. MAPEO SEGURO (Evitar desajuste de índices)
                        int count = Math.Min(names.Length, weightsValues.Length);
                        var top3 = Enumerable.Range(0, count)
                                             .Select(i => new { Name = names[i], Value = Math.Abs(weightsValues[i]) })
                                             .OrderByDescending(x => x.Value)
                                             .Take(3);

                        return string.Join(" | ", top3.Select(v => $"{v.Name} ({v.Value:F4})"));
                    }
                }

                return $"Modelo {lastTransformer.GetType().Name} no expone pesos internos.";
            }
            catch (Exception ex)
            {
                return $"Error de diagnóstico: {ex.Message}";
            }
        }

        public static DataTable GetRecords(string _command)
        {
            string connString = (@"encrypt=false;database=neo_trader;server=DESARROLLO\SQLEXPRESS;user=sa;password=08Z5il37;MultipleActiveResultSets=True");
            DataTable dtResponse = new DataTable();
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = _command;
                dtResponse.Load(cmd.ExecuteReader());
            }
            return dtResponse;
        }

        public static List<T> ConvertDataTableToList<T>(DataTable dt) where T : new()
        {
            List<T> list = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T obj = new T();
                foreach (DataColumn col in dt.Columns)
                {
                    var prop = obj.GetType().GetProperty(col.ColumnName);
                    if (prop != null && row[col] != DBNull.Value) { prop.SetValue(obj, row[col]); }
                }
                list.Add(obj);
            }
            return list;
        }

        /*
        public static async Task<List<AgentViewModelItem>> SaveData(List<AgentViewModelItem> records)
        {
            int _i = 0;
            using (SqlConnection connection = new SqlConnection(neoContext.connString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.mod_trader_data_save";

                foreach (AgentViewModelItem record in records)
                {
                    int ndx = 0;
                    foreach (HistoricalChartInfo item in record.Data.Prices)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@Symbol", item.Meta.Symbol);
                        cmd.Parameters.AddWithValue("@ShortName", item.Meta.ShortName);
                        cmd.Parameters.AddWithValue("@InstrumentType", item.Meta.InstrumentType);
                        cmd.Parameters.AddWithValue("@Currency", item.Meta.Currency);
                        cmd.Parameters.AddWithValue("@DatePrice", item.Date);
                        cmd.Parameters.AddWithValue("@Open", item.Open);
                        cmd.Parameters.AddWithValue("@RegularMarketVolume", item.Meta.RegularMarketVolume);
                        cmd.Parameters.AddWithValue("@Volume", item.Volume);
                        cmd.Parameters.AddWithValue("@FiftyTwoWeekLow", item.Meta.FiftyTwoWeekLow);
                        cmd.Parameters.AddWithValue("@FiftyTwoWeekHigh", item.Meta.FiftyTwoWeekHigh);
                        cmd.Parameters.AddWithValue("@RegularMarketDayLow", item.Meta.RegularMarketDayLow);
                        cmd.Parameters.AddWithValue("@RegularMarketDayHigh", item.Meta.RegularMarketDayHigh);
                        cmd.Parameters.AddWithValue("@Low", item.Low);
                        cmd.Parameters.AddWithValue("@High", item.High);
                        cmd.Parameters.AddWithValue("@Close", item.Close);
                        double _PercentageMovementPreviousDay = 0;
                        double _PercentageMovementPreviousWeek = 0;
                        double _PercentageMovementPreviousMonth = 0;
                        if (ndx > 0)
                        {
                            // Calcular % mov dia previo
                            _PercentageMovementPreviousDay = neoContext.DiffPercentage(Convert.ToDouble(item.Close), Convert.ToDouble(record.Data.Prices[ndx - 1].Close));

                            // Si es divisible por 7 calcular % mov semana previa
                            if (ndx > 7) { _PercentageMovementPreviousWeek = neoContext.DiffPercentage(Convert.ToDouble(item.Close), Convert.ToDouble(record.Data.Prices[(ndx - 7)].Close)); }

                            // Si es divisible por 30 calcular % mov mes previo
                            if (ndx > 30)
                            { _PercentageMovementPreviousMonth = neoContext.DiffPercentage(Convert.ToDouble(item.Close), Convert.ToDouble(record.Data.Prices[(ndx - 30)].Close)); }
                        }
                        cmd.Parameters.AddWithValue("@PercentageMovementPreviousDay", _PercentageMovementPreviousDay);
                        cmd.Parameters.AddWithValue("@PercentageMovementPreviousWeek", _PercentageMovementPreviousWeek);
                        cmd.Parameters.AddWithValue("@PercentageMovementPreviousMonth", _PercentageMovementPreviousMonth);
                        cmd.Parameters.AddWithValue("@SplitFactor", 0);
                        _i = Convert.ToInt32(cmd.ExecuteScalar());
                        ndx++;
                    }
                }
                Consolidate();
                return records;
            }
        }

        public static void Consolidate()
        {
            List<SymbolsViewModelItems> _simbolos = ConvertDataTableToList<SymbolsViewModelItems>(GetSymbols());
            using (SqlConnection connection = new SqlConnection(connString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = connection;
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.mod_trader_symbols_consolida";
                foreach (SymbolsViewModelItems record in _simbolos)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@Symbol", record.code);
                    cmd.ExecuteScalar();
                }
            }
        }
        */

    }
}
