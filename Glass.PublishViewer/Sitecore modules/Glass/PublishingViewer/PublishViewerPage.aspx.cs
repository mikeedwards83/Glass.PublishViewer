using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using Sitecore.Jobs;

namespace Glass.PublishViewer
{
    public partial class PublishViewerPage : Page
    {
        public PageModel Model { get; set; }


        private PublishJobManager _publishJobManager;

        public PublishViewerPage()
        {
            _publishJobManager = PublishJobManager.Instance;
        }

        protected override void OnLoad(EventArgs e)
        {
            string id = Request.QueryString["id"];

            switch (Request.QueryString["action"])
            {
                case "delete":
                    Delete(id);
                    break;
            }

            Model = new PageModel();

            Model.Jobs = GetJobs();
            Model.Stats = _publishJobManager.PublishingStats;
            Model.Targets = new PublishingTargets();
            Model.Targets.AverageTimePerItem =
                Sitecore.Configuration.Settings.GetDoubleSetting("Glass.PublishViewer.AverageTimePerItem", 0.03);

            base.OnLoad(e);
        }

        private void Delete(string id)
        {
            _publishJobManager.Delete(id);
        }

        public IEnumerable<JobEntity> GetJobs()
        {
            var jobs = _publishJobManager.PublishJobs;

            var finished = jobs.Where(x => x.Status == JobState.Finished)
                .OrderBy(x => x.QueueTime)
                .ThenBy(x => x.JobName)
                .ThenBy(x => x.JobCategory)
                .ThenBy(x => x.Status);

            var notFinished = jobs.Where(x => x.Status != JobState.Finished)
                .OrderBy(x => x.QueueTime)
                .ThenBy(x => x.JobName)
                .ThenBy(x => x.JobCategory)
                .ThenBy(x => x.Status);

            return finished.Union(notFinished);
        }

        public class PageModel
        {
            public IEnumerable<JobEntity> Jobs { get; set; }
            public PublishingStats Stats { get; set; }
            public PublishingTargets Targets { get; set; }
        }
    }
}
