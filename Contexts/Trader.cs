using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using NeoConsole.BaseClasses;
using NeoConsole.Classes;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TorchSharp.torch.utils;

namespace NeoConsole.Contexts
{
    public class Trader : Abstract
    {
        public static string connString = (@"encrypt=false;database=neo_trader;server=DESARROLLO\SQLEXPRESS;user=sa;password=08Z5il37;MultipleActiveResultSets=True");

        [CustomDescription("Testing de Modelo para Trading")]
        public void TraderAI()
        {
            var mlContext = new MLContext();
            IDataView dataView = default;
            int positivos;
            int negativos;
            string ReturnLabel;

            string queRegistros = $"SELECT * FROM dbo.mod_trader_data;";
            dataView = CargarDatos(mlContext, connString, queRegistros);

            var registros = mlContext.Data.CreateEnumerable<QueDataInv>(dataView, reuseRowObject: false).ToList();
            positivos = registros.Count(x => x.Sube == true);
            negativos = registros.Count(x => x.Sube == false);

            ReturnLabel = "Label";

            Console.WriteLine($"Datos cargados -> Positivos ({ReturnLabel}): {positivos} | Negativos: {negativos}");

            var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);
            //var split = mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2, samplingKeyColumnName: "Mora");
            var trainData = split.TrainSet.Preview();
            var testData = split.TestSet.Preview();

            // 1. Train inicial
            var model = TrainModel(allFeatures);

            // 2. Permutation importance
            var importance = mlContext.BinaryClassification
                .PermutationFeatureImportance(model, data);

            // 3. Filtrar features
            var selectedFeatures = importance
                .Where(f => f.AreaUnderRocCurve.Mean > 0.001)
                .Select(f => f.FeatureName)
                .ToList();

            // 4. Reentrenar
            var finalModel = TrainModel(selectedFeatures);


            var tuner = new HyperparameterTuner(mlContext);

            var result = tuner.Optimize(
                trainData,
                testData,
                selectedFeatures,
                trials: 50);

            var bestModel = result.model;
            var bestParams = result.bestParams;

            Console.WriteLine($"Best AUC: {result.bestScore}");

            var tuner = new BayesianTuner(mlContext);

            var result = tuner.Optimize(
                trainData,
                testData,
                selectedFeatures,
                iterations: 40);

            Console.WriteLine($"Best Score: {result.bestScore}");
        }

        public IDataView CargarDatos(MLContext mlContext, string connectionString, string queRegistros)
        {
            var listaResultados = new List<QueDataInv>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queRegistros, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // 1. Leer valores de la fila
                        bool SubeSN = false;
                        int QueSube = 0;
                        if (Convert.ToSingle(reader["Close"]) > Convert.ToSingle(reader["Open"]))
                        {
                            QueSube++;
                        }
                        if (Convert.ToSingle(reader["PercentageMovementPreviousDay"]) > 0)
                        {
                            QueSube++;
                        }
                        if (Convert.ToSingle(reader["PercentageMovementPreviousWeek"]) > 0)
                        {
                            QueSube++;
                        }
                        if (Convert.ToSingle(reader["PercentageMovementPreviousMonth"]) > 0)
                        {
                            QueSube++;
                        }
                        if (QueSube > 2)
                        {
                            SubeSN = true;
                        }


                        // 3. Agregar a la lista
                        listaResultados.Add(new QueDataInv
                        {
                            id_symbol = Convert.ToSingle(reader["id_symbol"]),
                            DatePrice = Convert.ToString(reader["DatePrice"]).Substring(0, 10),
                            Open = Convert.ToSingle(reader["Open"]),
                            RegularMarketVolume = Convert.ToSingle(reader["RegularMarketVolume"]),
                            Volume = Convert.ToSingle(reader["Volume"]),
                            FiftyTwoWeekLow = Convert.ToSingle(reader["FiftyTwoWeekLow"]),
                            FiftyTwoWeekHigh = Convert.ToSingle(reader["FiftyTwoWeekHigh"]),
                            RegularMarketDayLow = Convert.ToSingle(reader["RegularMarketDayLow"]),
                            RegularMarketDayHigh = Convert.ToSingle(reader["RegularMarketDayHigh"]),
                            Low = Convert.ToSingle(reader["Low"]),
                            High = Convert.ToSingle(reader["High"]),
                            Close = Convert.ToSingle(reader["Close"]),
                            PercentageMovementPreviousDay = Convert.ToSingle(reader["PercentageMovementPreviousDay"]),
                            PercentageMovementPreviousWeek = Convert.ToSingle(reader["PercentageMovementPreviousWeek"]),
                            PercentageMovementPreviousMonth = Convert.ToSingle(reader["PercentageMovementPreviousMonth"]),
                            Sube = SubeSN
                        });

                        // Guardar información de valor del primer día de la semana

                    }
                }
            }

            // 4. Convertir la lista final en un IDataView para ML.NET
            return mlContext.Data.LoadFromEnumerable(listaResultados);
        }
    }
}

public class HyperparameterTuner
{
    private readonly MLContext _ml;
    private readonly Random _rnd = new Random(42);

    public HyperparameterTuner(MLContext mlContext)
    {
        _ml = mlContext;
    }

    public (ITransformer model, LightGbmBinaryTrainer.Options bestParams, double bestScore)
        Optimize(IDataView trainData, IDataView testData, string[] features, int trials = 30)
    {
        double bestAuc = double.MinValue;
        double bestScore = 0;
        ITransformer bestModel = null;
        LightGbmBinaryTrainer.Options bestParams = null;

        for (int i = 0; i < trials; i++)
        {
            var options = SampleParameters();

            var model = TrainModel(trainData, features, options);

            var predictions = model.Transform(testData);
            var metrics = _ml.BinaryClassification.Evaluate(predictions);

            double auc = metrics.AreaUnderRocCurve;

            Console.WriteLine($"Trial {i + 1}: AUC = {auc}");

            if (auc > bestAuc)
            {
                bestAuc = auc;
                bestModel = model;
                bestParams = options;
                // Mejor que el Area Under ROC Curve para Finanzas
                bestScore = 0.7 * auc + 0.3 * metrics.PositivePrecision;

            }
        }

        return (bestModel, bestParams, bestAuc);
    }

    private LightGbmBinaryTrainer.Options SampleParameters()
    {
        return new LightGbmBinaryTrainer.Options
        {
            NumberOfLeaves = _rnd.Next(20, 150),
            LearningRate = 0.01 + _rnd.NextDouble() * 0.05,
            NumberOfIterations = _rnd.Next(200, 1000),
            MinimumExampleCountPerLeaf = _rnd.Next(10, 100),
            // MUY importantes:
            UseCategoricalSplit = false,
            HandleMissingValue = true,
            LabelColumnName = "Label",
            FeatureColumnName = "Features"
        };
    }

    private ITransformer TrainModel(IDataView data, string[] features, LightGbmBinaryTrainer.Options options)
    {
        var pipeline = _ml.Transforms
            .Concatenate("Features", features)
            .Append(_ml.BinaryClassification.Trainers.LightGbm(options));

        return pipeline.Fit(data);
    }
}

public class BayesianTuner
{
    private readonly MLContext _ml;
    private readonly Random _rnd = new Random(42);

    private List<(LightGbmBinaryTrainer.Options param, double score)> history = new();

    public BayesianTuner(MLContext ml)
    {
        _ml = ml;
    }

    public (LightGbmBinaryTrainer.Options bestParams, double bestScore) Optimize(
        IDataView trainData,
        IDataView testData,
        string[] features,
        int iterations = 50)
    {
        for (int i = 0; i < iterations; i++)
        {
            LightGbmBinaryTrainer.Options candidate;

            if (i < 10)
            {
                // Exploración inicial
                candidate = RandomSample();
            }
            else
            {
                // Explotación (bias hacia mejores)
                candidate = GuidedSample();
            }

            var score = Evaluate(trainData, testData, features, candidate);

            history.Add((candidate, score));

            Console.WriteLine($"Iter {i + 1} → Score: {score}");
        }

        var best = history.OrderByDescending(h => h.score).First();

        return (best.param, best.score);
    }

    private LightGbmBinaryTrainer.Options RandomSample()
    {
        return new LightGbmBinaryTrainer.Options
        {
            NumberOfLeaves = _rnd.Next(20, 150),
            LearningRate = 0.01 + _rnd.NextDouble() * 0.05,
            NumberOfIterations = _rnd.Next(300, 800),
            MinimumExampleCountPerLeaf = _rnd.Next(10, 100),
            // MUY importantes:
            UseCategoricalSplit = false,
            HandleMissingValue = true,
            LabelColumnName = "Label",
            FeatureColumnName = "Features"
        };
    }

    private LightGbmBinaryTrainer.Options GuidedSample()
    {
        var top = history
            .OrderByDescending(h => h.score)
            .Take(5)
            .Select(h => h.param)
            .ToList();

        var baseParam = top[_rnd.Next(top.Count)];

        return new LightGbmBinaryTrainer.Options
        {
            NumberOfLeaves = Perturb(baseParam.NumberOfLeaves ?? 31, 10, 150),
            LearningRate = Perturb(baseParam.LearningRate ?? 0.01, 0.01, 0.05),
            NumberOfIterations = Perturb(baseParam.NumberOfIterations, 300, 800),
            MinimumExampleCountPerLeaf = Perturb(baseParam.MinimumExampleCountPerLeaf ?? 10, 10, 100),
            // MUY importantes:
            UseCategoricalSplit = false,
            HandleMissingValue = true,
            LabelColumnName = "Label",
            FeatureColumnName = "Features"
        };
    }

    private double Perturb(double value, double min, double max)
    {
        double noise = (_rnd.NextDouble() - 0.5) * 0.1;
        double newVal = value * (1 + noise);
        return Clamp(newVal, min, max);
    }

    private int Perturb(int value, int min, int max)
    {
        int noise = _rnd.Next(-10, 10);
        int newVal = value + noise;
        return Clamp(newVal, min, max);
    }

    private int Clamp(int value, int min, int max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private double Clamp(double value, double min, double max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    private double Evaluate(IDataView train, IDataView test, string[] features, LightGbmBinaryTrainer.Options options)
    {
        var pipeline = _ml.Transforms
            .Concatenate("Features", features)
            .Append(_ml.BinaryClassification.Trainers.LightGbm(options));

        var model = pipeline.Fit(train);

        var predictions = model.Transform(test);

        var metrics = _ml.BinaryClassification.Evaluate(predictions);

        return metrics.AreaUnderRocCurve;
    }
}
}
