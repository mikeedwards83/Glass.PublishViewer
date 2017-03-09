using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Publishing.Pipelines.PublishItem;

namespace Glass.PublishViewer.Pipelines.PublishItem
{
    public  class Terminator: Sitecore.Publishing.Pipelines.PublishItem.PublishItemProcessor
    {
        public override void Process(PublishItemContext context)
        {
            PublishJobManager.Instance.CheckTermination();
        }

       
    }
}
