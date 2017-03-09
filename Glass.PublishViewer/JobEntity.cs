using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Jobs;

namespace Glass.PublishViewer
{
    public class JobEntity
    {
        /// <summary>
        /// Gets or sets the handle.
        /// </summary>
        /// <value>The handle.</value>
        public Handle Handle { get; set; }
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>The message.</value>
        public List<string> Message { get; set; }
        /// <summary>
        /// Gets or sets the name of the job.
        /// </summary>
        /// <value>The name of the job.</value>
        public string JobName { get; set; }
        /// <summary>
        /// Gets or sets the job category.
        /// </summary>
        /// <value>The job category.</value>
        public string JobCategory { get; set; }
        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        /// <value>The name of the item.</value>
        public string ItemName { get; set; }
        /// <summary>
        /// Gets or sets the item ID.
        /// </summary>
        /// <value>The item ID.</value>
        public string ItemID { get; set; }
        /// <summary>
        /// Gets or sets the source database.
        /// </summary>
        /// <value>The source database.</value>
        public string SourceDatabase { get; set; }
        /// <summary>
        /// Gets or sets the target database.
        /// </summary>
        /// <value>The target database.</value>
        public IEnumerable<string> TargetDatabase { get; set; }
        /// <summary>
        /// Gets or sets the current target database.
        /// </summary>
        /// <value>The current target database.</value>
        public string CurrentTargetDatabase { get; set; }
        /// <summary>
        /// Gets or sets the languages.
        /// </summary>
        /// <value>
        /// The languages.
        /// </value>
        public IEnumerable<string> Languages { get; set; }
        /// <summary>
        /// Gets or sets the current language.
        /// </summary>
        /// <value>The current language.</value>
        public string CurrentLanguage { get; set; }
        /// <summary>
        /// Gets or sets the job status.
        /// </summary>
        /// <value>The job status.</value>
        public JobState Status { get; set; }
        /// <summary>
        /// Gets or sets the processed.
        /// </summary>
        /// <value>The processed.</value>
        public long Processed { get; set; }
        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        public string Mode { get; set; }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        public bool IsSingleItem { get; set; }
        /// <summary>
        /// Gets or sets the child count.
        /// </summary>
        /// <value>The child count.</value>
        public int ChildCount { get; set; }


        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        /// <summary>
        /// Gets or sets the percentage.
        /// </summary>
        /// <value>The percentage.</value>
        public int Percentage
        {
            get
            {
                if (ChildCount < 0)
                {
                    return -1;
                }
                var percentage= (int) (((((double) Processed)/((double) ChildCount)))*(double)100);
                return percentage > 100 ? 100 : percentage;
            }
        }

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>The owner.</value>
        public string Owner { get; set; }
        /// <summary>
        /// Gets or sets the queue time.
        /// </summary>
        /// <value>
        /// The queue time.
        /// </value>
        public DateTime QueueTime { get; set; }

        public TimeSpan ProcessingDuration
        {
            get
            {
                if (StartTime.HasValue)
                {
                    if (EndTime.HasValue)
                    {
                        return EndTime.Value - StartTime.Value;

                    }
                    return DateTime.UtcNow - StartTime.Value;
                }

                return TimeSpan.Zero;
            }
        }
        public TimeSpan QueueDuration
        {
            get
            {
                if (StartTime.HasValue)
                {
                        return StartTime.Value - QueueTime;
                }

                return DateTime.UtcNow - QueueTime;

            }
        }


        public int SkippedItems
        {
            get
            {
                if (Message.Any())
                {
                    var skippeds = Message.Where(x => x.Contains("skipped")).Select(x =>
                    {
                        int skipped = 0;

                        var match = _skippedMatch.Match(x);
                        if (match != null)
                        {
                            var group = match.Groups["count"];

                            if (group != null)
                            {
                                int.TryParse(group.Value, out skipped);
                            }
                        }

                        return skipped;
                    });

                    if (skippeds.Any())
                    {
                        return skippeds.Aggregate((x, y) => x + y);
                    }
                }
                return 0;
            }
        }

        public double AverageTimePerItem
        {
            get
            {
                if (Processed > 0)
                {
                    return (ProcessingDuration.TotalSeconds / (double)(Processed - SkippedItems));
                }

                return 0;
            }
        }

        public bool StatsProcessed { get; set; }

        Regex _skippedMatch = new Regex(@"(?<count>\d+)");

        public JobEntity()
        {
            Languages = new List<string>();
            TargetDatabase = new List<string>();
            Message = new List<string>();
            ChildCount = -1;
        }
    }
}
