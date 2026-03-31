using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using NeoConsole.BaseClasses;
using NeoConsole.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxTokenParser;

namespace NeoConsole.Contexts
{
    public class AIModel : Abstract
    {
        // 1. Definición de la estructura del CSV
        public class EquipoData
        {
            [LoadColumn(0)] public string Genero { get; set; }
            [LoadColumn(1)] public float Edad { get; set; }
            [LoadColumn(2)] public float Peso { get; set; }
            [LoadColumn(3)] public string Talla { get; set; }
            [LoadColumn(4)] public float Comida { get; set; }
            [LoadColumn(5)] public float IMC { get; set; }

            // Columna de destino (Label) - Supongamos que queremos predecir si es "Saludable"
            // Para BinaryClassification, esta columna DEBE ser booleana.
            [LoadColumn(6)] public bool Saludable { get; set; }
        }

        public class EquipoPrediction
        {
            [ColumnName("PredictedLabel")] public bool Prediccion { get; set; }
            public float Probability { get; set; }
            public float Score { get; set; }
        }

        class Program
        {
            static void Main(string[] args)
            {
                var mlContext = new MLContext(seed: 1);
                string dataPath = "datos_pacientes.csv"; // El archivo debe estar en la carpeta /bin

                // 2. Carga desde CSV
                IDataView dataView = mlContext.Data.LoadFromTextFile<EquipoData>(
                    path: dataPath,
                    hasHeader: true,
                    separatorChar: ','
                );

                // 3. División Estratificada (80% Entrenamiento / 20% Prueba)
                var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, samplingKeyColumnName: "Saludable");

                // 4. Pipeline de Transformación y Limpieza
                var pipeline = mlContext.Transforms.ReplaceMissingValues(new[] {
                    new InputOutputColumnPair("Edad"),
                    new InputOutputColumnPair("Talla"),
                    new InputOutputColumnPair("IMC")
                }, Microsoft.ML.Transforms.MissingValueReplacingEstimator.ReplacementMode.Mean)

                    // Convertimos categorías de texto a números (OneHot)
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("ComidaEncoded", "Comida"))
                    .Append(mlContext.Transforms.Categorical.OneHotEncoding("GeneroEncoded", "Genero"))

                    // Normalización de Precio y RAM (tienen escalas muy distintas a CPUs)
                    .Append(mlContext.Transforms.NormalizeMinMax("Peso"))
                    .Append(mlContext.Transforms.NormalizeMinMax("Talla"))

                    // Unimos todas las Features
                    .Append(mlContext.Transforms.Concatenate("Features", "GeneroEncoded", "Edad", "Peso", "Talla", "ComidaEncoded", "IMC"));

                // 5. Configuración de Hiperparámetros (L1 y L2)
                var options = new SdcaLogisticRegressionBinaryTrainer.Options
                {
                    LabelColumnName = "Saludable",
                    FeatureColumnName = "Features",
                    L1Regularization = 0.03f, // Elimina ruido de marcas/provincias no relevantes
                    L2Regularization = 0.01f, // Estabiliza el peso de los precios altos
                    MaximumNumberOfIterations = 500
                };

                var trainingPipeline = pipeline.Append(mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(options));

                // 6. Entrenamiento y Evaluación
                Console.WriteLine("Iniciando entrenamiento...");
                var model = trainingPipeline.Fit(split.TrainSet);

                var predictions = model.Transform(split.TestSet);
                var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "EsGamaAlta");

                // 7. Salida de Resultados
                MostrarMetricas(metrics);
            }

            private static void MostrarMetricas(BinaryClassificationMetrics metrics)
            {
                Console.WriteLine("\n--- RESULTADOS DE EVALUACIÓN ---");
                Console.WriteLine($"Precisión (Accuracy): {metrics.Accuracy:P2}");
                Console.WriteLine($"AUC (Área bajo la curva): {metrics.AreaUnderRocCurve:P2}");
                Console.WriteLine($"F1 Score: {metrics.F1Score:P2}");
                Console.WriteLine("\nMatriz de Confusión:");
                Console.WriteLine(metrics.ConfusionMatrix.GetFormattedConfusionTable());
            }
        }
    }
}
