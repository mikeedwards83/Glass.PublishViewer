using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Reflection;

namespace Glass.PublishViewer
{
    public class PublishJobManager
    {

        private string SingleItem = "singleitem";
        private static Hashtable htChildCount = new Hashtable();

        private static ConcurrentDictionary<string, JobEntity> _jobEntities = new ConcurrentDictionary<string, JobEntity>();
        private static System.Timers.Timer _processingTimer;

        public static PublishJobManager Instance { get; set; }


        private static MethodInfo DeleteMethod;
        private static FieldInfo JobOptionMethodInstanceField;
        private string PublishCancelledMessage { get; set; }

        public PublishingStats PublishingStats { get; private set; }
        public MethodInstance PublishByPassMethodInstance { get; private set; }
      

        public IEnumerable<JobEntity> PublishJobs
        {
            get { return _jobEntities.Values; }
        }


        static PublishJobManager()
        {
            GetMethods();

            int processingInterval = 1000 * 1;

            Instance = new PublishJobManager();

            _processingTimer = new System.Timers.Timer(processingInterval);
            _processingTimer.Elapsed += (obj, args) => Instance.JobRefresh();
            _processingTimer.AutoReset = true;
            _processingTimer.Start();
        }


        public PublishJobManager()
        {
            PublishingStats = new PublishingStats();
            PublishByPassMethodInstance = new MethodInstance(this, "PublishByPass", null);
            PublishCancelledMessage =
                Sitecore.Configuration.Settings.GetSetting("Glass.PublishViewer.AverageTimePerItem", "Publish Cancelled");
        }

        protected static void GetMethods()
        {
            DeleteMethod = typeof(JobManager).GetMethod("RemoveJob",
                            BindingFlags.Static |
                            BindingFlags.NonPublic);

            JobOptionMethodInstanceField = typeof(JobOptions).GetField("method", BindingFlags.NonPublic|BindingFlags.Instance);
;        }

        protected virtual void JobRefresh()
        {
            IEnumerable<Job> jobs = JobManager.GetJobs()
                .Where(x => x.Name.ToLower().Equals("publish"))
                .ToArray();

            var handles = _jobEntities.Keys.ToList();

            foreach (var job in jobs)
            {
                var jobDetail = new JobEntity();

                var handleStr = job.Handle.ToString();
                var containsHandle = handles.Contains(handleStr);
                if (containsHandle)
                {
                    jobDetail = _jobEntities[handleStr];
                    handles.Remove(handleStr);
                }

                MapJobEntity(job, jobDetail);

                if (!containsHandle)
                {
                    PublishingStats.NumberOfPublishes++;

                    _jobEntities.TryAdd(handleStr, jobDetail);
                }
            }

            foreach (var handle in handles)
            {
                JobEntity expiredJob;
                _jobEntities.TryGetValue(handle, out expiredJob);

                if (expiredJob.EndTime == null)
                {
                    expiredJob.EndTime = DateTime.UtcNow;
                }
            }


            var removeJobs = _jobEntities.Where(x =>x.Value.EndTime.HasValue);

            //remove any remaining handle
            foreach (var job in removeJobs)
            {

                JobEntity finishedEntity;

                if (job.Value.EndTime < DateTime.UtcNow.AddMinutes(-2))
                {
                    _jobEntities.TryRemove(job.Key, out finishedEntity);
                }
                else
                {
                    _jobEntities.TryGetValue(job.Key, out finishedEntity);
                }

                if (finishedEntity != null && finishedEntity.StatsProcessed == false)
                {
                    finishedEntity.StatsProcessed = true;
                    UpdateStats(finishedEntity);
                }

            }


            PublishingStats.NumberOfQueuedPublishes = _jobEntities.Count(x => x.Value.Status == JobState.Queued);

        }

        protected virtual void UpdateStats(JobEntity jobEntity)
        {
            

            if (jobEntity.StartTime.HasValue)
            {

                var queueTime = PublishingStats.AverageQueueTime;
                var jobQueueTime = jobEntity.QueueDuration;
                var newQueueAverage = GetAverage(
                    (long)queueTime.TotalSeconds, 
                    PublishingStats.NumberOfCompletedPublishes,
                    (long)jobQueueTime.TotalSeconds,
                    1);
               
                PublishingStats.AverageQueueTime = new TimeSpan(0,0, (int)newQueueAverage);

                if (jobEntity.EndTime.HasValue)
                {
                    var currentItemCount = PublishingStats.ItemsPublished;
                    var jobItemCount = jobEntity.Processed;
                    var itemDuration =
                        PublishingStats.AverageTimePerItem;

                    var jobItemSeconds = jobEntity.ProcessingDuration.TotalSeconds; //we only care to 2 decimal

                    var newItemAverage = GetAverage(itemDuration, currentItemCount,  jobItemSeconds,
                        jobItemCount);
                    PublishingStats.AverageTimePerItem = Double.IsInfinity(newItemAverage) ? 0 : newItemAverage; 
                }

            }

            if (jobEntity.Status == JobState.Finished)
            {
                PublishingStats.NumberOfCompletedPublishes++;
                PublishingStats.ItemsPublished += jobEntity.Processed;
            }


        }

        protected double GetAverage(double currentAverage, double currentCount, double newValue, double increment)
        {
            var currentTotal = currentAverage*currentCount;
            currentTotal += newValue;
            return currentTotal/(currentCount + increment);
        }

        protected JobEntity MapJobEntity(Job job, JobEntity jobDetail)
        {

            PublishOptions[] publishOptions = (Sitecore.Publishing.PublishOptions[]) (job.Options.Parameters[0]);
            PublishStatus publishStatus = ((Sitecore.Publishing.PublishStatus) (job.Options.Parameters[1]));

            jobDetail.Handle = job.Handle;
            jobDetail.JobName = job.Name;
            jobDetail.JobCategory = job.Category;
            jobDetail.QueueTime = job.QueueTime;

            jobDetail.Mode = publishOptions[0].Mode.ToString();
            jobDetail.IsSingleItem = jobDetail.Mode.ToLower().Equals(SingleItem);
            
            var currentStatus = publishStatus.State;

            if (currentStatus != JobState.Queued && jobDetail.StartTime == null)
            {
                jobDetail.StartTime = DateTime.UtcNow;
            }
            if (currentStatus == JobState.Finished && jobDetail.EndTime == null)
            {
                jobDetail.EndTime = DateTime.UtcNow;
            }
            if (currentStatus == JobState.Running)
            {
                jobDetail.EndTime = null;
            }

            jobDetail.Status = currentStatus;

            if (!String.IsNullOrEmpty(job.Options.ContextUser.Name))
                jobDetail.Owner = job.Options.ContextUser.Name.ToString();

            if (publishStatus.CurrentLanguage != null)
                jobDetail.CurrentLanguage = publishStatus.CurrentLanguage.CultureInfo.DisplayName.ToString();

            if (publishStatus.CurrentTarget != null)
                jobDetail.CurrentTargetDatabase = publishStatus.CurrentTarget.Name.ToString();

            jobDetail.Processed = publishStatus.Processed;

            if (jobDetail.IsSingleItem)
            {
                jobDetail.ItemName = publishOptions[0].RootItem.Paths.Path;

                if (jobDetail.ChildCount == -1)
                {
                    int languageCount = publishOptions.Length;

                    var childCount = (CalcChildCount(publishOptions[0].RootItem, publishOptions[0].Deep) + 1 + 1);
                    jobDetail.ChildCount = (childCount * languageCount);
                }

                jobDetail.Processed =publishStatus.Processed;
            }
            else
            {
                jobDetail.ItemName = "Full Site";
            }

            jobDetail.SourceDatabase = publishOptions[0].SourceDatabase.Name.ToString();

            List<string> targetDbs = new List<string>();
            List<string> targetLanguages = new List<string>();

            foreach (PublishOptions publishoptions in publishOptions)
            {
                if (!targetDbs.Contains(publishoptions.TargetDatabase.Name))
                    targetDbs.Add(publishoptions.TargetDatabase.Name);

                if (!targetLanguages.Contains(publishoptions.Language.CultureInfo.Name))
                    targetLanguages.Add(publishoptions.Language.CultureInfo.Name);

                var messages = publishStatus.Messages;

                if (messages.Count > 0)
                {
                    if (jobDetail.Message.Count < messages.Count)
                    {
                        for (int i = jobDetail.Message.Count; i < messages.Count; i++)
                        {
                            var message = messages[i];
                            jobDetail.Message.Add(message);
                        }
                    }
                }
            }


            jobDetail.TargetDatabase = targetDbs.ToArray();

            jobDetail.Languages =targetLanguages;

            return jobDetail;
            
        }

        /// <summary>
        /// Calcs the child count.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="nLevelchild">if set to <c>true</c> [n levelchild].</param>
        /// <returns></returns>
        protected int CalcChildCount(Item item, bool nLevelchild)
        {
            int childCount = 0;
            if (item != null)
            {
                if (item.HasChildren && nLevelchild)
                {
                    childCount = item.Children.Count;

                    foreach (Item child in item.Children)
                    {
                        childCount += CalcChildCount(child, nLevelchild);
                    }
                }
            }
            return childCount;
        }

        public void Delete(string handleStr)
        {
            if (_jobEntities.ContainsKey(handleStr))
            {
                JobEntity jobEntity = null;
                _jobEntities.TryGetValue(handleStr, out jobEntity);

                if (jobEntity != null &&jobEntity.Status == JobState.Queued)
                {
                    var job = JobManager.GetJob(jobEntity.Handle);
                    if (job != null)
                    {
                        PublishingStats.NumberOfCancelledPublishes++;
                        var publishStatus = ((Sitecore.Publishing.PublishStatus)(job.Options.Parameters[1]));
                        publishStatus.SetState(JobState.Finished);
                        publishStatus.Messages.Add(PublishCancelledMessage);
                        jobEntity.StartTime = DateTime.UtcNow;

                        //this is a really hacky way of hijacking the publishing process
                        JobOptionMethodInstanceField.SetValue(job.Options, PublishByPassMethodInstance);

                        // DeleteMethod.Invoke(null, new[] {job});
                    }
                }
            }
        }

        public void PublishByPass()
        {
            //this method is empty and is used to stop a publish occuring
        }
    }
}
