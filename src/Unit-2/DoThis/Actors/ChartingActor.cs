using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor
    {
        #region Messages

        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                this.Series = series;
            }
        }

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        #endregion

        private readonly Chart chart;
        private Dictionary<string, Series> seriesIndex;

        public ChartingActor(Chart chart) : this(chart, new Dictionary<string, Series>())
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex)
        {
            this.chart = chart;
            this.seriesIndex = seriesIndex;

            this.Receive<InitializeChart>(ic => this.HandleInitialize(ic));
            this.Receive<AddSeries>(addSeries => this.HandleAddSeries(addSeries));
        }

        #region Individual Message Type Handlers

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) &&
                !this.seriesIndex.ContainsKey(series.Series.Name))
            {
                this.seriesIndex.Add(series.Series.Name, series.Series);
                this.chart.Series.Add(series.Series);
            }
        }

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                //swap the two series out
                this.seriesIndex = ic.InitialSeries;
            }

            //delete any existing series
            this.chart.Series.Clear();

            //attempt to render the initial chart
            if (this.seriesIndex.Any())
            {
                foreach (var series in this.seriesIndex)
                {
                    //force both the chart and the internal index to use the same names
                    series.Value.Name = series.Key;
                    this.chart.Series.Add(series.Value);
                }
            }
        }

        #endregion
    }
}
