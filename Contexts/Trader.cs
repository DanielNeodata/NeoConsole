using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers.LightGbm;
using NeoConsole.BaseClasses;
using NeoConsole.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using static TorchSharp.torch.utils;

namespace NeoConsole.Contexts
{
    public class Trader : Abstract
    {
        public static string connString = (@"encrypt=false;database=neo_trader;server=localhost;user=sa;password=08Z5il37;MultipleActiveResultSets=True");

        [CustomDescription("Testing de Modelo para Trading")]
        public void TraderAI()
        {
            var mlContext = new MLContext();
            IDataView dataView = default;
            int positivos;
            int negativos;

            string queRegistros = $"SELECT * FROM dbo.mod_trader_data WHERE DatePrice < '2021-01-01' ORDER BY id_symbol ASC, DatePrice ASC;";
            dataView = CargarDatos(mlContext, connString, queRegistros);

            var registros = mlContext.Data.CreateEnumerable<StockData>(dataView, reuseRowObject: false).ToList();
            positivos = registros.Count(x => x.Label == true);
            negativos = registros.Count(x => x.Label == false);


            Console.WriteLine($"Datos cargados -> Positivos (Label): {positivos} | Negativos: {negativos}");

            var allData = mlContext.Data
                .CreateEnumerable<StockData>(dataView, false)
                .OrderBy(x => x.Date)
                .ToList();

            int splitIndex = (int)(allData.Count * 0.8);

            var trainList = allData.Take(splitIndex).ToList();
            var testList = allData.Skip(splitIndex).ToList();
            var trainData = mlContext.Data.LoadFromEnumerable(trainList);
            var testData = mlContext.Data.LoadFromEnumerable(testList);

            string[] allFeatures = new[]
            {
                nameof(StockData.Ret1D),
                nameof(StockData.Ret5D),
                nameof(StockData.Ret10D),
                nameof(StockData.Ret20D),
                nameof(StockData.PriceSma10),
                nameof(StockData.PriceSma20),
                nameof(StockData.PriceSma50),
                nameof(StockData.Volatility10),
                nameof(StockData.Volatility20),
                nameof(StockData.VolumeRatio),
                nameof(StockData.Rsi),
                nameof(StockData.BbPosition),
                nameof(StockData.DistMax),
                nameof(StockData.DistMin)
            };

            var pipeline = mlContext.Transforms.Concatenate("Features", allFeatures)
                        .Append(mlContext.BinaryClassification.Trainers.LightGbm(new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options
                        {
                            NumberOfLeaves = 64,
                            LearningRate = 0.02,
                            NumberOfIterations = 500,
                            MinimumExampleCountPerLeaf = 50,
                        }));

            // 1. Train inicial
            var trainer = new ModelTrainer(mlContext);
            var model = trainer.TrainModel(dataView, allFeatures);
            Console.WriteLine("Entrenamiento inicial");

            // 2. Permutation importance
            var transformedData = model.Transform(dataView);

            // Obtener el último paso del pipeline (LightGBM)
            // Opción 1: Usar IEnumerable (más flexible)
            var transformers = (IEnumerable<ITransformer>)model;
            var lastTransformer = transformers.Last();

            double baseline = EvaluateAuc(mlContext, model, dataView);
            Console.WriteLine($"Baseline: {baseline}");

            var importances = new Dictionary<string, double>();

            foreach (var feature in allFeatures)
            {
                var shuffled = ShuffleFeature(mlContext, dataView, feature);

                var predictions = model.Transform(shuffled);

                var metrics = mlContext.BinaryClassification.Evaluate(predictions);

                var newAuc = metrics.AreaUnderRocCurve;

                importances[feature] = baseline - newAuc;
                Console.WriteLine($"Feature: {feature} => AUC: {newAuc}");
            }

            // 3. Filtrar features
            var selectedFeatures = importances
                .Where(f => f.Value > 0.001)
                .Select(f => f.Key)
                .ToList();

            // 4. Reentrenar
            if (selectedFeatures.Count > 0)
            {
                var finalModel = trainer.TrainModel(dataView, selectedFeatures.ToArray());
                Console.WriteLine("Modelo mejorado");
            } else
            {
                var finalModel = trainer.TrainModel(dataView, allFeatures.ToArray());
                Console.WriteLine("Modelo básico");
            }

            var tunerA = new HyperparameterTuner(mlContext);

            if (selectedFeatures.Count > 0)
            {
                var resultA = tunerA.Optimize(
                trainData,
                testData,
                selectedFeatures.ToArray(),
                trials: 50);
                var bestModel = resultA.model;
                var bestParams = resultA.bestParams;
                Console.WriteLine($"Optimizacion por hiperparametros - Best AUC: {resultA.bestScore}");
            }
            else
            {
                var resultA = tunerA.Optimize(
                trainData,
                testData,
                allFeatures.ToArray(),
                trials: 50);
                var bestModel = resultA.model;
                var bestParams = resultA.bestParams;
                Console.WriteLine($"Optimizacion por hiperparametros - Best AUC: {resultA.bestScore}");
            }


            var tunerB = new BayesianTuner(mlContext);

            if (selectedFeatures.Count > 0)
            {
                var resultB = tunerB.Optimize(
                trainData,
                testData,
                selectedFeatures.ToArray(),
                iterations: 40);
                Console.WriteLine($"Optimizacion bayesiana - Best Score: {resultB.bestScore}");
            }
            else
            {
                var resultB = tunerB.Optimize(
                trainData,
                testData,
                allFeatures.ToArray(),
                iterations: 40);
                Console.WriteLine($"Optimizacion bayesiana - Best Score: {resultB.bestScore}");
            }
        }

        private double EvaluateAuc(MLContext mlContext, ITransformer model, IDataView data)
        {
            var predictions = model.Transform(data);

            var metrics = mlContext.BinaryClassification.Evaluate(predictions);

            return metrics.AreaUnderRocCurve;
        }

        public IDataView ShuffleFeature(MLContext mlContext, IDataView data, string columnName)
        {
            var rows = mlContext.Data.CreateEnumerable<StockData>(data, false).ToList();

            Random rng = new Random();
            PropertyInfo prop = typeof(StockData).GetProperty(columnName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

            if (prop == null)
                throw new ArgumentException($"Property '{columnName}' not found on type {typeof(StockData)}");

            int n = rows.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);

                // Swap based on column value (optional logic, e.g., compare or just randomize)
                // For pure randomization, just swap elements
                StockData value = rows[k];
                rows[k] = rows[n];
                rows[n] = value;
            }

            return mlContext.Data.LoadFromEnumerable(rows);
        }

        public IDataView CargarDatos(MLContext mlContext, string connectionString, string queRegistros)
        {
            var listaRegistros = new List<StockDB>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand(queRegistros, connection);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Datos basicos; Retorno; Medias Moviles; Volatilidad; Volumen; RSI y Bollinger; Derivadas
                        // Tick, Date; Ret1D, Ret5D, Ret10D, Ret20D; PriceSma10, PriceSma20, PriceSma50; Volatility10; VolumeRatio; Rsi, BbPosition; DistMax, DistMin;

                        // 3. Agregar a la lista
                        listaRegistros.Add(new StockDB
                        {
                            id_symbol = Convert.ToSingle(reader["id_symbol"]),
                            DatePrice = Convert.ToDateTime(reader["DatePrice"]),
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
                            PercentageMovementPreviousMonth = Convert.ToSingle(reader["PercentageMovementPreviousMonth"])
                        });
                    }
                }
            }

            List<StockData> listaResultados = TransformaDatos.Transforma(listaRegistros);

            // 4. Convertir la lista final en un IDataView para ML.NET
            return mlContext.Data.LoadFromEnumerable(listaResultados);
        }
    }
}

public class ModelTrainer
{
    private readonly MLContext _ml;

    public ModelTrainer(MLContext mlContext)
    {
        _ml = mlContext;
    }

    public ITransformer TrainModel(IDataView data, string[] features)
    {
        var pipeline = _ml.Transforms
            .Concatenate("Features", features)
            .Append(_ml.BinaryClassification.Trainers.LightGbm(
                new Microsoft.ML.Trainers.LightGbm.LightGbmBinaryTrainer.Options
                {
                    NumberOfLeaves = 64,
                    LearningRate = 0.02,
                    NumberOfIterations = 500,
                    MinimumExampleCountPerLeaf = 50,
                }));

        return pipeline.Fit(data);
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

    public class TuningResult
    {
        public LightGbmBinaryTrainer.Options Param { get; set; }
        public double Score { get; set; }
    }

    private List<TuningResult> history = new List<TuningResult>();

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

            history.Add(new TuningResult
            {
                Param = candidate,
                Score = score
            });

            Console.WriteLine($"Iter {i + 1} → Score: {score}");
        }

        var best = history.OrderByDescending(h => h.Score).First();

        return (best.Param, best.Score);
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
            .OrderByDescending(h => h.Score)
            .Take(5)
            .Select(h => h.Param)
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