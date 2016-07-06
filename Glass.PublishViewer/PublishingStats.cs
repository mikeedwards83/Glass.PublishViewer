using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Glass.PublishViewer
{
    public class PublishingStats
    {
        public PublishingStats()
        {
            MonitorStart = DateTime.UtcNow;
            AverageQueueTime =TimeSpan.Zero;
            
        }
        public DateTime MonitorStart { get; set; }
        public int NumberOfPublishes { get; set; }
        public int NumberOfCancelledPublishes { get; set; }
        public int NumberOfCompletedPublishes { get; set; }
        public TimeSpan AverageQueueTime { get; set; }
        public double AverageTimePerItem { get; set; }
        public long ItemsPublished { get; set; }

        public TimeSpan MonitoringDuration
        {
            get { return DateTime.UtcNow - MonitorStart; }
        }

       
        public int NumberOfQueuedPublishes { get; set; }

        
    }
}
