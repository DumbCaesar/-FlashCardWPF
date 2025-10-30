using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FlashCardWPF.Model
{
    public class DailyStats
    {
        public string Date { get; set; }
        public double TotalStudyTimeSeconds { get; set; }

        public int TotalCards { get; set; }

        [JsonIgnore]
        public TimeSpan TotalStudyTime
        {
            get => TimeSpan.FromSeconds(TotalStudyTimeSeconds);
            set => TotalStudyTimeSeconds = value.TotalSeconds;
        }

        [JsonIgnore]
        public TimeSpan AverageTimePerCard => 
            TotalCards > 0 ? TimeSpan.FromSeconds(TotalStudyTimeSeconds / TotalCards) 
            : TimeSpan.Zero;
    } 
}
