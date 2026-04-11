using Microsoft.ML.Data;
using NeoConsole.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
namespace NeoConsole.Classes
{
    public class StockDB
    {
        //﻿cuotas;monto;edad;ingresos;empresa;comerciante;comercio;sucursal;plan;sexo;ocupacion;calificacion;nacionalidad;localidad
        [LoadColumn(0)] public float id_symbol { get; set; }
        [LoadColumn(1)] public DateTime DatePrice { get; set; }
        [LoadColumn(2)] public float Open { get; set; }
        [LoadColumn(3)] public float RegularMarketVolume { get; set; }
        [LoadColumn(4)] public float Volume { get; set; }
        [LoadColumn(5)] public float FiftyTwoWeekLow { get; set; }
        [LoadColumn(6)] public float FiftyTwoWeekHigh { get; set; }
        [LoadColumn(7)] public float RegularMarketDayLow { get; set; }
        [LoadColumn(8)] public float RegularMarketDayHigh { get; set; }
        [LoadColumn(9)] public float Low { get; set; }
        [LoadColumn(10)] public float High { get; set; }
        [LoadColumn(11)] public float Close { get; set; }
        [LoadColumn(12)] public float PercentageMovementPreviousDay { get; set; }
        [LoadColumn(13)] public float PercentageMovementPreviousWeek { get; set; }
        [LoadColumn(14)] public float PercentageMovementPreviousMonth { get; set; }

        // Columna de destino (Label) - Supongamos que queremos predecir si es "Saludable"
        // Para BinaryClassification, esta columna DEBE ser booleana.
        [LoadColumn(15)] public bool Sube { get; set; }
    }

    public class StockData
    {
        // Datos basicos
        [LoadColumn(0)] public float Tick { get; set; }
        [LoadColumn(1)] public DateTime Date { get; set; }
        // Retorno
        [LoadColumn(2)] public float Ret1D { get; set; }
        [LoadColumn(3)] public float Ret5D { get; set; }
        [LoadColumn(4)] public float Ret10D { get; set; }
        [LoadColumn(5)] public float Ret20D { get; set; }
        // Medias moviles
        [LoadColumn(6)] public float PriceSma10 { get; set; }
        [LoadColumn(7)] public float PriceSma20 { get; set; }
        [LoadColumn(8)] public float PriceSma50 { get; set; }
        // Volatilidad
        [LoadColumn(9)] public float Volatility10 { get; set; }
        [LoadColumn(10)] public float Volatility20 { get; set; }
        // Volumen
        [LoadColumn(11)] public float VolumeRatio { get; set; }
        // RSI y Bollinger
        [LoadColumn(12)] public float Rsi { get; set; }
        [LoadColumn(13)] public float BbPosition { get; set; }
        // Derivadas
        [LoadColumn(14)] public float DistMax { get; set; }
        [LoadColumn(15)] public float DistMin { get; set; }
        // Salida
        [LoadColumn(16)] public bool Label { get; set; } // target
    }

    public static class TransformaDatos
    {
        public static List<StockData> Transforma(List<StockDB> listaRegistros)
        {
            var listaResultados = new List<StockData>();
            float quetick = 0;
            int qtc = 0;
            for (int t = 0; t < listaRegistros.Count; t++)
            {
                // Sobre que "Tick" va a trabajar
                if (quetick != listaRegistros[t].id_symbol)
                {
                    quetick = listaRegistros[t].id_symbol;
                    qtc = 1;
                }
                else
                {
                    qtc++;
                }

                // Retorno
                float ret_1d = 0.0f;
                float ret_5d = 0.0f;
                float ret_10d = 0.0f;
                float ret_20d = 0.0f;
                if (qtc > 1)
                {
                    ret_1d = (listaRegistros[t].Close / listaRegistros[(t - 1)].Close) - 1;
                }
                if (qtc > 5)
                {
                    ret_5d = (listaRegistros[t].Close / listaRegistros[t - 5].Close) - 1;
                }
                if (qtc > 10)
                {
                    ret_10d = (listaRegistros[t].Close / listaRegistros[t - 10].Close) - 1;
                }
                if (qtc > 20)
                {
                    ret_20d = (listaRegistros[t].Close / listaRegistros[t - 20].Close) - 1;
                }

                // Medias moviles
                float psma_10 = 0.0f;
                float psma_20 = 0.0f;
                float psma_50 = 0.0f;
                if (qtc > 9)
                {
                    psma_10 = listaRegistros[t].Close / (listaRegistros.Skip(t - 10).Take(10).Sum(x => x.Close) / 10);
                }
                if (qtc > 19)
                {
                    psma_20 = listaRegistros[t].Close / (listaRegistros.Skip(t - 20).Take(20).Sum(x => x.Close) / 20);
                }
                if (qtc > 49)
                {
                    psma_50 = listaRegistros[t].Close / (listaRegistros.Skip(t - 50).Take(50).Sum(x => x.Close) / 50);
                }

                // Volatilidad
                float vol_10 = 0.0f;
                float vol_20 = 0.0f;
                if (qtc > 9)
                {
                    double avg = (listaResultados.Skip(t - 9).Take(9).Sum(x => x.Ret1D) + ret_1d) / 10;
                    double sumOfSquares = listaResultados.Skip(t - 9).Take(9).Sum(x => ((x.Ret1D - avg) * (x.Ret1D - avg))) + (ret_1d * ret_1d);
                    vol_10 = (float)Math.Sqrt((sumOfSquares / 10));
                }
                if (qtc > 19)
                {
                    double avg = (listaResultados.Skip(t - 19).Take(19).Sum(x => x.Ret1D) + ret_1d) / 20;
                    double sumOfSquares = listaResultados.Sum(x => ((x.Ret1D - avg) * (x.Ret1D - avg))) + (ret_1d * ret_1d);
                    vol_20 = (float)Math.Sqrt((sumOfSquares / 20));
                }

                // Volumen
                float vol_avg_20 = 0.0f;
                float vol_ratio = 0.0f;
                if (qtc > 19)
                {
                    vol_avg_20 = (listaRegistros.Skip(t - 19).Take(20).Sum(x => x.Volume) / 20);
                    vol_ratio = listaRegistros[t].Volume / vol_avg_20;
                }

                // RSI
                float rsi = 0.0f;
                if (qtc > 14)
                {
                    var closes = listaRegistros.Skip(t - 14).Take(15).Select(x => x.Close).ToList();
                    var rsiValues = CalculateRsi(closes, 14);
                    rsi = rsiValues[14];
                }

                // Bollinger bands
                float bb_mid = 0.0f;
                float bb_std = 0.0f;
                float bb_upper = 0.0f;
                float bb_lower = 0.0f;
                float bb_pos = 0.0f;
                if (qtc > 19)
                {
                    bb_mid = (listaRegistros.Skip(t - 19).Take(19).Sum(x => x.Close) + ret_1d) / 20;
                    double sumOfSquares = listaRegistros.Skip(t - 19).Take(19).Sum(x => Math.Pow(x.Close - bb_mid, 2)) + ((ret_1d - bb_mid) * (ret_1d - bb_mid));
                    bb_std = (float)Math.Sqrt((sumOfSquares / 20));
                    bb_upper = bb_mid + 2 * bb_std;
                    bb_lower = bb_mid - 2 * bb_std;
                    bb_pos = (listaRegistros[t].Close - bb_lower) / (bb_upper - bb_lower);
                }

                // Derivadas
                float max_20 = 0.0f;
                float min_20 = 0.0f;
                float dist_max = 0.0f;
                float dist_min = 0.0f;
                if (qtc > 19)
                {
                    max_20 = listaRegistros.Skip(t - 19).Take(20).Max(x => x.Close);
                    min_20 = listaRegistros.Skip(t - 19).Take(20).Min(x => x.Close);
                    dist_max = (listaRegistros[t].Close / max_20) - 1;
                    dist_min = (listaRegistros[t].Close / min_20) - 1;
                }

                //Mercado
                //sp_ret_5d = SP500[t] / SP500[t-5] - 1
                //relative_strength = ret_5d - sp_ret_5d

                // 7 y 14 dias
                bool QueLabel = false;
                if (qtc > 7)
                {
                    float futureReturn = (listaRegistros[t].Close / listaRegistros[t - 7].Close) - 1;
                    if (futureReturn > 0.02f)
                    {
                        QueLabel = true;
                    }
                }
                //if return > +2% → 1
                //if return < -2 % → 0
                //else → ignorar fila

                // 3. Agregar a la lista
                // Datos basicos; Retorno; Medias Moviles; Volatilidad; Volumen; RSI y Bollinger; Derivadas
                // Tick, Date; Ret1D, Ret5D, Ret20D; PriceSma10, PriceSma20; Volatility10; VolumeRatio; Rsi, BbPosition; DistMax, DistMin;
                listaResultados.Add(new StockData
                {
                    Tick = quetick,
                    Date = listaRegistros[t].DatePrice,
                    Ret1D = ret_1d,
                    Ret5D = ret_5d,
                    Ret10D = ret_10d,
                    Ret20D = ret_20d,
                    PriceSma10 = psma_10,
                    PriceSma20 = psma_20,
                    PriceSma50 = psma_50,
                    Volatility10 = vol_10,
                    Volatility20 = vol_20,
                    VolumeRatio = vol_ratio,
                    Rsi = rsi,
                    BbPosition = bb_pos,
                    DistMax = dist_max,
                    DistMin = dist_min,
                    Label = QueLabel
                });
            }

            return listaResultados;
        }

        public static List<float> CalculateRsi(List<float> closes, int period = 14)
        {
            var rsi = new List<float>(new float[closes.Count]);

            float gain = 0;
            float loss = 0;

            // Primer promedio simple
            for (int i = 1; i < period; i++)
            {
                float change = closes[i] - closes[i - 1];

                if (change > 0)
                    gain += change;
                else
                    loss -= change;
            }

            float avgGain = gain / period;
            float avgLoss = loss / period;

            // Primer RSI válido
            rsi[period] = avgLoss == 0 ? 100 : 100 - (100 / (1 + avgGain / avgLoss));

            // RSI suavizado (Wilder)
            for (int i = period + 1; i < closes.Count; i++)
            {
                float change = closes[i] - closes[i - 1];

                float currentGain = change > 0 ? change : 0;
                float currentLoss = change < 0 ? -change : 0;

                avgGain = (avgGain * (period - 1) + currentGain) / period;
                avgLoss = (avgLoss * (period - 1) + currentLoss) / period;

                if (avgLoss == 0)
                    rsi[i] = 100;
                else
                {
                    float rs = avgGain / avgLoss;
                    rsi[i] = 100 - (100 / (1 + rs));
                }
            }

            return rsi;
        }
    }
}
