using Microsoft.ML;
using Microsoft.ML.AutoML;
using Microsoft.ML.Calibrators;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using NeoConsole.BaseClasses;
using NeoConsole.Classes;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Schema;


namespace NeoConsole.Contexts
{
    public class QueData
    {
        //﻿cuotas;monto;edad;ingresos;empresa;comerciante;comercio;sucursal;plan;sexo;ocupacion;calificacion;nacionalidad;localidad
        [LoadColumn(0)] public float Cuotas { get; set; }
        [LoadColumn(1)] public float Monto { get; set; }
        [LoadColumn(2)] public float Edad { get; set; }
        [LoadColumn(3)] public float Ingresos { get; set; }
        [LoadColumn(4)] public float Empresa { get; set; }
        [LoadColumn(5)] public float Comerciante { get; set; }
        [LoadColumn(6)] public float Comercio { get; set; }
        [LoadColumn(7)] public float Sucursal { get; set; }
        [LoadColumn(8)] public float Plan { get; set; }
        [LoadColumn(9)] public float Sexo { get; set; }
        [LoadColumn(10)] public float Ocupacion { get; set; }
        [LoadColumn(11)] public float Calificacion { get; set; }
        [LoadColumn(12)] public float Nacionalidad { get; set; }
        [LoadColumn(13)] public float Localidad { get; set; }

        // Columna de destino (Label) - Supongamos que queremos predecir si es "Saludable"
        // Para BinaryClassification, esta columna DEBE ser booleana.
        [LoadColumn(14)] public bool Mora { get; set; }
    }

    public class DataPrediction
    {
        [ColumnName("PredictedLabel")] public bool Prediccion { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }

        // Esta es la propiedad que causaba el error si estaba fuera de lugar
        [ColumnName("FeatureContributions")]
        public float[] FeatureContributions { get; set; }
    }

    public class AITest : Abstract
    {

        [CustomDescription("Prueba del AutoML")]
        public void AutoModelAI(bool brief = false, bool ToFile = false)
        {
            //float l1 = 0.0f;
            //float l2 = 0.0f;
            //int iter = 0;
            string QueHace = "e";
            string MezclaSN = "s";
            if (!brief)
            {
                Console.WriteLine($"Parametros: $1 {QueHace} | $2: {MezclaSN}");
            }

            StreamWriter QueFile = new StreamWriter("D:\\Ruben\\www\\neodata.code\\NeoConsole\\salida.csv", append: true);
            QueFile.WriteLine("L1,L2,Precision,AUC,F1.Score");

            var mlContext = new MLContext();
            string dataPath = "D:\\Ruben\\www\\neodata.code\\NeoConsole\\Datos\\universo.csv";

            // 2. Carga desde CSV
            IDataView dataView = mlContext.Data.LoadFromTextFile<QueData>(
                path: dataPath,
                hasHeader: true,
                separatorChar: ','
            );

            // 1. Verifica los datos
            var registros = mlContext.Data.CreateEnumerable<QueData>(dataView, reuseRowObject: false).ToList();
            int positivos = registros.Count(x => x.Mora == true);
            int negativos = registros.Count(x => x.Mora == false);

            Console.WriteLine($"Datos cargados -> Positivos (Mora): {positivos} | Negativos: {negativos}");

            if (QueHace == "e")
            {

                // 2. Configuración del Experimento de AutoML
                // Buscamos maximizar el AUC para que la evaluación sea integral
                var settings = new BinaryExperimentSettings
                {
                    MaxModels = 10,
                    MaxExperimentTimeInSeconds = 600, // 5 minutos de búsqueda intensa
                    OptimizingMetric = BinaryClassificationMetric.AreaUnderRocCurve
                };

                var experiment = mlContext.Auto().CreateBinaryClassificationExperiment(settings);

                // 3. Ejecución del experimento
                Console.WriteLine("AutoML explorando algoritmos (FastTree, LightGBM, SDCA, etc.)...");

                // En lugar de pasar el dataView directo, puedes intentar barajarlo primero
                var shuffledData = mlContext.Data.ShuffleRows(dataView);

                // Ejecutar el experimento
                ExperimentResult<BinaryClassificationMetrics> result;
                if (MezclaSN == "s")
                {
                    result = experiment.Execute(shuffledData, labelColumnName: "Mora");
                }
                else
                {
                    result = experiment.Execute(dataView, labelColumnName: "Mora");
                }

                // Llamada al método que imprime los pesos de las variables en la consola
                MostrarPesosEnConsola(mlContext, result.BestRun.Model, dataView);

                // 4. Extracción de los mejores Hiperparámetros
                var bestRun = result.BestRun;
                Console.WriteLine($"\n--- MEJOR MODELO ENCONTRADO ---");
                Console.WriteLine($"Algoritmo: {bestRun.TrainerName}");
                Console.WriteLine($"AUC: {bestRun.ValidationMetrics.AreaUnderRocCurve:P2}");
                Console.WriteLine($"F1 Score: {bestRun.ValidationMetrics.F1Score:P2}");

                // 5. El modelo final ya incluye la limpieza y normalización interna
                var finalModel = bestRun.Model;

                Console.WriteLine("\n===============================================");
                Console.WriteLine($"GANADOR: {bestRun.TrainerName}");
                Console.WriteLine($"Métrica (AUC): {bestRun.ValidationMetrics.AreaUnderRocCurve:P2}");
                Console.WriteLine("===============================================\n");

                var metrics = bestRun.ValidationMetrics;
                Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());

                // 6. Extraer Hiperparámetros específicos
                // Nota: Dependiendo del algoritmo (FastTree, SDCA, etc.), los parámetros varían.
                if (bestRun.TrainerName.Contains("Sdca"))
                {
                    // Si el ganador es SDCA, podemos ver L1 y L2
                    Console.WriteLine("--- Detalles de Regularización ---");
                    // En algunas versiones se accede vía result.BestRun.Estimator
                    Console.WriteLine($"Algoritmo lineal detectado.");
                    Console.WriteLine("AutoML ajustó los pesos para ignorar variables con poco impacto.");
                }
                else if (bestRun.TrainerName.Contains("FastTree") || bestRun.TrainerName.Contains("LightGbm"))
                {
                    // Si el ganador es basado en árboles
                    Console.WriteLine("--- Configuración de Árboles ---");
                    Console.WriteLine("El modelo detectó relaciones no lineales (complejas) entre tus datos.");
                    ITransformer bestModel = result.BestRun.Model;
                    PrintTopFeatures(bestModel, dataView, "Mora");
                }

                // 7. Mostrar el tiempo que le tomó decidir
                Console.WriteLine($"\nTiempo de búsqueda: {bestRun.RuntimeInSeconds:F2} segundos");

                // 8. Guardar el modelo para uso futuro
                mlContext.Model.Save(finalModel, dataView.Schema, "ModeloDatosOptimo.zip");
                Console.WriteLine("\nModelo guardado como 'ModeloDatosOptimo.zip'");

                /*
                            // 8.5 Obtener datos de las variables de decisiones
                            // 8.5.1. Extraer el predictor real de la cadena de transformaciones
                            var chain = (TransformerChain<ITransformer>)bestRun.Model;
                            var predictionTransformer = chain.Last() as ISingleFeaturePredictionTransformer<object>;

                            // 8.5.2. Calcular PFI (Importancia por permutación)
                            // Nota: No necesitas un namespace "Explainability", esto es parte de BinaryClassification
                            var pfiResults = mlContext.BinaryClassification.GetPermutationFeatureImportance(
                                predictionTransformer,
                                bestRun.Model.Transform(dataView), // Datos ya procesados
                                labelColumnName: "Saludable",
                                numberOfIterations: 1);

                            // 8.5.3. Mostrar resultados en consola
                            var top3 = pfiResults
                                .Select(x => new { Variable = x.Key, Impacto = Math.Abs(x.Value.AreaUnderRocCurve.Mean) })
                                .OrderByDescending(x => x.Impacto)
                                .Take(3);

                            foreach (var v in top3)
                            {
                                Console.WriteLine($"Variable: {v.Variable} | Impacto: {v.Impacto:F4}");
                            }
                */
                // 9. Guardar el Log
                long totalRegistros = dataView.GetColumn<bool>("Mora").Count();
                string detalleEstimator = bestRun.Estimator.ToString();
                string topVariables = Tools.ObtenerTopVariables(mlContext, bestRun.Model, dataView);

                // 10. Guardar todo en el log
                GuardarLogDeParametros(
                    "log_tecnico_ml.csv",
                    "Datos_Final",
                    bestRun.ValidationMetrics,
                    bestRun.TrainerName,
                    bestRun.RuntimeInSeconds,
                    bestRun.Estimator.ToString(),
                    dataView.GetColumn<bool>("Mora").Count(),
                    topVariables
                );

                Console.WriteLine($"\n--- TOP VARIABLES DETECTADAS ---");
                Console.WriteLine(topVariables);
                Console.WriteLine($"Log actualizado: {totalRegistros} registros procesados con {bestRun.TrainerName}.");

                // 11. Prueba de predicción
                // Creamos un motor de predicción basado en el mejor modelo encontrado
                var predictionEngine = mlContext.Model.CreatePredictionEngine<QueData, DataPrediction>(bestRun.Model);
                var outputSchema = predictionEngine.OutputSchema;

                // Creamos un perfil de prueba (ajusta los valores para probar)
                //﻿cuotas;monto;edad;ingresos;empresa;comerciante;comercio;sucursal;plan;sexo;ocupacion;calificacion;nacionalidad;localidad
                var prueba = new QueData
                {
                    // CON MORA
                    // True;0,06;0,0022;0,24;0,4571;0,1;0,056;0,4129;0,01;0,2233;0,147;0,0133;0,0015;0,3466;0,0161
                    /*
                    Cuotas = 0.06f,
                    Monto = 0.0022f,
                    Edad = 0.24f,
                    Ingresos = 0.4571f,
                    Empresa = 0.1f,
                    Comerciante = 0.056f,
                    Comercio = 0.4129f,
                    Sucursal = 0.01f,
                    Plan = 0.2233f,
                    Sexo = 0.147f,
                    Ocupacion = 0.0133f,
                    Calificacion = 0.0015f,
                    Nacionalidad = 0.3466f,
                    Localidad = 0.0161f
                    */
                    // SIN MORA
                    // False;0,12;0,0846;0,63;0,0118;0,1;0,0752;0,3373;0,35;0,2125;0,147;0,2068;0,0016;0,3466;0,5332
                    Cuotas = 0.12f,
                    Monto = 0.0846f,
                    Edad = 0.63f,
                    Ingresos = 0.0118f,
                    Empresa = 0.1f,
                    Comerciante = 0.0752f,
                    Comercio = 0.3373f,
                    Sucursal = 0.35f,
                    Plan = 0.2125f,
                    Sexo = 0.147f,
                    Ocupacion = 0.2068f,
                    Calificacion = 0.0016f,
                    Nacionalidad = 0.3466f,
                    Localidad = 0.5332f
                };

                var resultado = predictionEngine.Predict(prueba);

                Console.WriteLine("\n--- TEST DE PREDICCIÓN ---");
                Console.WriteLine($"Perfil: Cuotas {prueba.Cuotas}, Monto {prueba.Monto:F2}, Edad {prueba.Edad}");
                Console.WriteLine($"¿Tendrá Mora?: {(resultado.Prediccion ? "SÍ" : "NO")}");
                Console.WriteLine($"Probabilidad: {resultado.Probability:P2}");
                Console.WriteLine("\n--- Variables de prediccion ---");

                // Retrieve the labels from the OutputSchema
                var labelColumn = predictionEngine.OutputSchema["PredictedLabel"];
                VBuffer<ReadOnlyMemory<char>> keyNames = default;

                // Las etiquetas suelen estar en "KeyValues" para columnas de tipo Key
                if (labelColumn.Annotations.Schema.GetColumnOrNull("KeyValues") != null)
                {
                    labelColumn.Annotations.GetValue("KeyValues", ref keyNames);
                    var labels = keyNames.DenseValues().Select(x => x.ToString()).ToArray();
                    Console.WriteLine($"Clases detectadas: {string.Join(", ", labels)}");
                }
                //var labelBuffer = new VBuffer<ReadOnlyMemory<char>>();
                //predictionEngine.OutputSchema["Score"].Annotations.GetValue("SlotNames", ref labelBuffer);

                //labels = labelBuffer.DenseValues().Select(l => l.ToString()).ToArray();

                // Solo para modelos Multi-Class
                //var index = Array.IndexOf(labels, resultado.Prediccion);
                //var score = resultado.Score[index];
                //
                // Now use the retrieved labels to create the dictionary
                //var top10scores = labels
                //   .ToDictionary(
                //        l => l,
                //       l => (decimal)resultado.Score[Array.IndexOf(labels, l)]
                //    )
                //    .OrderByDescending(kv => kv.Value)
                //    .Take(10);
                //
                //foreach (var pair in top10scores)
                //{
                //    Console.WriteLine($"Key: {pair.Key}, Value: {pair.Value}");
                //}

            }
            else
            {
                // 3. División Estratificada (80% Entrenamiento / 20% Prueba)
                var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.1);
                //var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, samplingKeyColumnName: "Mora");
                var previewTrain = split.TrainSet.Preview();
                var previewTest = split.TestSet.Preview();

                /*
                // 4. Pipeline de Transformación y Limpieza
                var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] {
                        new InputOutputColumnPair("Edad"),
                        new InputOutputColumnPair("Peso"),
                        new InputOutputColumnPair("Talla"),
                        new InputOutputColumnPair("IMC")
                }, Microsoft.ML.Transforms.MissingValueReplacingEstimator.ReplacementMode.Mean)

                // Convertimos categorías de texto a números (OneHot)
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("ComidaEncoded", "Comida"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("GeneroEncoded", "Genero"))
                .Append(mlContext.Transforms.Categorical.OneHotEncoding("EstructuraEncoded", "Estructura"))

                // Normalización
                .Append(mlContext.Transforms.NormalizeMinMax("Edad"))
                .Append(mlContext.Transforms.NormalizeMinMax("Peso"))
                .Append(mlContext.Transforms.NormalizeMinMax("Talla"))
                .Append(mlContext.Transforms.NormalizeMinMax("IMC"))
                */

                //l1 = 0.025f;
                //l2 = 0.003f;
                //                for (l1 = 0.0f; l1 < 0.3f; l1 = l1 + 0.001f)
                //                {
                //for (l2 = 0.0f; l2 < 0.3f; l2 = l2 + 0.001f)
                //{
                // Unimos todas las Features
                //﻿cuotas;monto;edad;ingresos;empresa;comerciante;comercio;sucursal;plan;sexo;ocupacion;calificacion;nacionalidad;localidad
                var pipeline = mlContext.Transforms.Concatenate("Features", "Cuotas", "Monto", "Edad", "Ingresos", "Empresa", "Comerciante", "Comercio", "Sucursal", "Plan", "Sexo", "Ocupacion", "Calificacion", "Nacionalidad", "Localidad");

                // 5. Configuración de Hiperparámetros (L1 y L2)
                /*
                var options = new SdcaLogisticRegressionBinaryTrainer.Options
                {
                    LabelColumnName = "Mora",
                    FeatureColumnName = "Features",
                    L1Regularization = l1, // Elimina ruido de campos no relevantes
                    L2Regularization = l2, // Estabiliza el peso de los precios altos
                    MaximumNumberOfIterations = 500
                };
                */
                var options = new LightGbmBinaryTrainer.Options
                {
                    LabelColumnName = "Mora",
                    FeatureColumnName = "Features",
                    NumberOfLeaves = 31,
                    MinimumExampleCountPerLeaf = 20,
                    LearningRate = 0.05,
                    NumberOfIterations = 300,
                    L2CategoricalRegularization = 0.1

                    /*
                     * Para N < 10k filas
                    NumberOfLeaves = 15;
                    MinimumExampleCountPerLeaf = 30;
                    L2CategoricalRegularization = 1;
                    FeatureFraction = 0.7;
                    */
                    /*
                     * Para 10k < N < 100k filas
                    NumberOfLeaves = 31;
                    MinimumExampleCountPerLeaf = 20;
                    L2CategoricalRegularization = 0.1;
                    FeatureFraction = 0.8;
                    */
                    /*
                     * Mejor version para Microsoft.ML.LightGbm
                    NumberOfLeaves = 31,
                    MinimumExampleCountPerLeaf = 20,
                    LearningRate = 0.05,
                    NumberOfIterations = 300,
                    L2CategoricalRegularization = 0.1
                    */
                };

                //var trainingPipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(options));
                var trainingPipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.LightGbm(options));

                // 6. Entrenamiento y Evaluación
                if (!brief)
                {
                    Console.WriteLine("Iniciando entrenamiento...");
                }
                var model = trainingPipeline.Fit(split.TrainSet);

                /*
                Console.WriteLine("--- Datos del modelo utilizado ---");
                Console.WriteLine(model.GetType());
                Console.WriteLine(model.LastTransformer.GetType());
                var last = model.LastTransformer;
                Console.WriteLine(last.GetType().FullName);
                Console.WriteLine("-----------------------");
                */

                PrintTopFeatures(model, dataView, "Mora");

                var predictions = model.Transform(split.TestSet);
                //var previewFull = dataView.Preview();
                var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "Mora");

                /*
                iter++;
                Console.WriteLine(iter);
                
                // 7. Salida de Resultados
                if (!brief)
                {
                    Console.WriteLine("--- Hiperparámetros ---");
                    Console.WriteLine($"L1: {l1} - L2: {l2}");
                }
                MostrarMetricas(metrics, l1, l2, QueFile, brief, true);
                if (!brief)
                {
                    Console.WriteLine("-----------------------");
                }
                */
            }
            //               }
            //           }
            // Borra la funcion para tomar lo enviado como parámetro
            QueFile.Close();
            QueHace = "";
        }

        public static void PrintTopFeatures(
            ITransformer model,
            IDataView data,
            string featureColumnName = "Features",
            int topN = 0)
        {
            try
            {
                var chain = (TransformerChain<ITransformer>)model;
                var lastTransformer = chain.Last();
                //var chain = model as TransformerChain<
                //    BinaryPredictionTransformer<
                //        CalibratedModelParametersBase<
                //        LightGbmBinaryModelParameters,
                //        PlattCalibrator>>>;
                if (lastTransformer == null)
                {
                    Console.WriteLine("El modelo no es del tipo esperado.");
                    return;
                } else
                {
                    Console.WriteLine("--- Datos del modelo utilizado ---");
                    Console.WriteLine(model.GetType());
                    Console.WriteLine(lastTransformer.GetType());
                    Console.WriteLine(lastTransformer.GetType().FullName);
                    Console.WriteLine("-----------------------");
                }

                // Casteamos al tipo de predicción binaria genérica
                // Usamos 'dynamic' para evitar escribir toda la firma genérica gigante que pusiste arriba
                dynamic binaryTransformer = lastTransformer;
                var calibratedModel = binaryTransformer.Model;

                // El modelo calibrado tiene una propiedad llamada 'SubModel'
                // Ahí es donde vive realmente el FastTree o LightGBM
                var actualModel = calibratedModel.SubModel;

                // 4. Ahora sí, extraemos las importancias (FastTree utiliza Feature Weights)
                if (actualModel is Microsoft.ML.Trainers.FastTree.FastTreeBinaryModelParameters fastTreeModel)
                {
                    VBuffer<float> weights = default;
                    fastTreeModel.GetFeatureWeights(ref weights);

                    var values = weights.DenseValues().ToArray();

                    var total = values.Sum();

                    // Aquí ya puedes listar los pesos como antes
                    Console.WriteLine("Pesos de FastTree (dentro del calibrador) extraídos con éxito.");

                    // Transformar datos
                    var transformedData = model.Transform(data);
                    var column = transformedData.Schema[featureColumnName];

                    //var nombres = weights.DenseValues().Select(x => x.ToString()).ToArray();
                    var nombres = weights.DenseValues().Select((val, index) => $"{index}: {val}").ToArray();
                    if (topN == 0)
                    {
                        topN = nombres.Length;
                    }
                    var ranking = nombres.Select((name, index) => new {
                        Nombre = "Columna: " + index.ToString().PadLeft(3),
                        Valor = values[index]
                    })
                                .OrderByDescending(x => x.Valor)
                                .Take(topN);

                    Console.WriteLine("Orden Columna | Importancia (Gain) | Peso   ");
                    Console.WriteLine("--------------------------------------------");

                    foreach (var item in ranking)
                    {
                        Console.WriteLine($"{item.Nombre,-13} |       {item.Valor:F4}       | {(item.Valor * 100 / total).ToString().PadLeft(6)}%");
                        // 'i' representa el orden o índice de la columna en el set de datos procesado
                        //Console.WriteLine($"{i.ToString().PadRight(5)} | {values[i]} | {values[i]*100/total}%");
                    }
                }
                if (actualModel is Microsoft.ML.Trainers.LightGbm.LightGbmBinaryModelParameters)
                {
//                    chain = (TransformerChain<ITransformer>)model;
//                    lastTransformer = chain.Last();

                    // 2. Acceder al modelo calibrado usando dynamic para simplificar la jerarquía
//                    binaryTransformer = lastTransformer;
//                    calibratedModel = binaryTransformer.Model;

                    // 3. Extraer el submodelo (el motor de LightGBM)
//                    actualModel = calibratedModel.SubModel;
                    
                    VBuffer<float> weights = default;
                    actualModel.GetFeatureWeights(ref weights);
                    var values = weights.DenseValues().ToArray();

                    float total = values.Sum();

                    var nombres = weights.DenseValues().Select(x => x.ToString()).ToArray();
                    if (topN == 0)
                    {
                        topN = nombres.Length;
                    }
                    var ranking = nombres.Select((name, index) => new {
                        Nombre = "Columna: " + index.ToString().PadLeft(3),
                        Valor = values[index]
                    })
                                .OrderByDescending(x => x.Valor)
                                .Take(topN);

                    Console.WriteLine("Orden Columna | Importancia (Gain) | Peso   ");
                    Console.WriteLine("--------------------------------------------");

                    foreach (var item in ranking)
                    {
                        Console.WriteLine($"{item.Nombre,-13} |       {item.Valor:F4}       | {(item.Valor*100/total).ToString().PadLeft(6)}%");
                        // 'i' representa el orden o índice de la columna en el set de datos procesado
                        //Console.WriteLine($"{i.ToString().PadRight(5)} | {values[i]} | {values[i]*100/total}%");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al extraer importancia de LightGBM: {ex.Message}");
            }
        }

        /*
        static void MostrarPesosSDCA(MLContext mlContext, ITransformer model, IDataView data)
        {
            try
            {
                // 1. Obtener el esquema para los nombres de las columnas
                var column = model.Transform(data).Schema["Features"];
                VBuffer<ReadOnlyMemory<char>> slotNames = default;
                column.GetSlotNames(ref slotNames);
                var nombres = slotNames.DenseValues().Select(x => x.ToString()).ToArray();

                // 2. Extraer el modelo lineal del pipeline
                // En un TransformerChain, el último es el predictor
                var chain = (TransformerChain<ITransformer>)model;
                var predictionTransformer = chain.Last() as ISingleFeaturePredictionTransformer<object>;

                // 3. CAST CRUCIAL: Convertir a los parámetros de LinearBinaryModelParameters
                var modelParams = predictionTransformer.Model as Microsoft.ML.Trainers.LinearBinaryModelParameters;

                if (modelParams != null)
                {
                    // SDCA tiene una propiedad 'Weights' que es un VBuffer<float>
                    var weights = modelParams.Weights;
                    var valoresPesos = weights.DenseValues().ToArray();

                    // 4. Unir y mostrar el Top 3
                    var ranking = nombres.Select((name, index) => new {
                        Nombre = name,
                        Peso = valoresPesos[index]
                    })
                                        .OrderByDescending(x => Math.Abs(x.Peso))
                                        .Take(5);

                    Console.WriteLine("\n=== IMPACTO DE VARIABLES (SDCA LOGISTIC) ===");
                    foreach (var item in ranking)
                    {
                        string sentido = item.Peso > 0 ? "[Aumenta Mora]" : "[Reduce Mora]";
                        Console.WriteLine($" {sentido} {item.Nombre,-15} : {item.Peso:F4}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al extraer pesos de SDCA: " + ex.Message);
            }
        }
        */
        static void MostrarPesosEnConsola(MLContext mlContext, ITransformer model, IDataView data)
        {
            try
            {
                // 1. Obtenemos el esquema real (con la columna 'Features')
                var transformedData = model.Transform(data);
                var column = transformedData.Schema["Features"];

                // 2. Extraemos los nombres de las columnas originales
                VBuffer<ReadOnlyMemory<char>> slotNames = default;
                column.GetSlotNames(ref slotNames);
                var names = slotNames.DenseValues().Select(x => x.ToString()).ToArray();

                // 3. Navegamos por la cadena del modelo hasta el algoritmo final
                var chain = (TransformerChain<ITransformer>)model;
                var lastTransformer = chain.Last();

                // 4. TRUCO DE REFLEXIÓN: Buscamos la propiedad 'Model' y luego 'Weights'
                var modelProp = lastTransformer.GetType().GetProperty("Model");
                if (modelProp != null)
                {
                    var modelParams = modelProp.GetValue(lastTransformer);
                    var weightsProp = modelParams.GetType().GetProperty("Weights")
                                   ?? modelParams.GetType().GetProperty("FeatureWeights");

                    if (weightsProp != null)
                    {
                        var weights = (VBuffer<float>)weightsProp.GetValue(modelParams);
                        var weightsArray = weights.DenseValues().ToArray();

                        // 5. Unimos nombres con pesos y ordenamos
                        var top3 = names.Select((name, index) => new { Nombre = name, Valor = Math.Abs(weightsArray[index]) })
                                        .OrderByDescending(x => x.Valor)
                                        .Take(3);

                        Console.WriteLine("\n>>> ANALISIS DE VARIABLES (NEODATA ML) <<<");
                        foreach (var v in top3)
                        {
                            Console.WriteLine($" * {v.Nombre,-20} : {v.Valor:F4}");
                        }
                        return;
                    }
                }
                Console.WriteLine("El algoritmo actual no permite extraer pesos directamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Aviso técnico: {ex.Message}");
            }
        }

        private void GuardarLogDeParametros(string archivoLog, string nombreModelo, BinaryClassificationMetrics metricas, string trainerName, double tiempoSegundos, string estimatorFull, long cantidadRegistros, string topFeatures) // Nueva columna
        {
            if (!File.Exists(archivoLog))
            {
                string cabecera = "Fecha,Modelo,Algoritmo,AUC,Registros,TiempoSeg,TopFeatures,Estimador\n";
                File.WriteAllText(archivoLog, cabecera, Encoding.UTF8);
            }

            string linea = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}," +
                           $"{nombreModelo}," +
                           $"{trainerName}," +
                           $"{metricas.AreaUnderRocCurve:F4}," +
                           $"{cantidadRegistros}," +
                           $"{tiempoSegundos:F2}," +
                           $"\"{topFeatures}\"," +
                           $"\"{estimatorFull.Replace("\"", "'")}\"\n";

            File.AppendAllText(archivoLog, linea, Encoding.UTF8);
        }

        private static void MostrarMetricas(BinaryClassificationMetrics metrics, float hpl1, float hpl2, StreamWriter QueFile, bool brief = false, bool ToFile = false)
        {
            if (!ToFile)
            {

                if (!brief)
                {
                    Console.WriteLine("\n--- RESULTADOS DE EVALUACIÓN ---");
                    Console.WriteLine($"Precisión (Accuracy): {metrics.Accuracy:P2}");
                    Console.WriteLine($"AUC (Área bajo la curva): {metrics.AreaUnderRocCurve:P2}");
                    Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");
                    Console.WriteLine("\nMatriz de Confusión:");
                    Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
                }
                else
                {
                    if (metrics.F1Score > 0.1f)
                    {
                        Console.WriteLine($"{hpl1},{hpl2},{metrics.Accuracy:P2},{metrics.AreaUnderRocCurve:P2},{metrics.F1Score:P2}");
                    }
                }
            }
            else
            {
                if (metrics.F1Score > 0.1f)
                {
                    QueFile.WriteLine($"{hpl1},{hpl2},{metrics.Accuracy:P2},{metrics.AreaUnderRocCurve:P2},{metrics.F1Score:P2}");
                }
            }
        }
    }
}