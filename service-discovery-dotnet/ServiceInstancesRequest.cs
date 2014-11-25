using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_discovery
{
    public class ServiceInstancesRequest
    {
        private readonly string _Resource;
        private readonly IEnumerable<string> _Tags;

        public ServiceInstancesRequest(string resource, IEnumerable<string> tags = null)
        {
            _Resource = resource;
            _Tags = tags ?? Enumerable.Empty<string>();
        }

        public string Resource
        {
            get { return _Resource; }
        }

        public IEnumerable<string> Tags
        {
            get { return _Tags; }
        }
    }
}
