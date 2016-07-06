using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Pipelines;

namespace Glass.PublishViewer.Pipelines.Initialise
{
    public class StartPublishMonitor
    {
        public void Process(PipelineArgs args)
        {
            //calling instance starts the manager
            var instance =PublishJobManager.Instance;
        }
    }
}
