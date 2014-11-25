using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace service_discovery
{
    public interface IServiceRegistryRepository
    {
        IEnumerable<ServiceInstance> GetServiceInstancesForResource(string resource);

        ServiceInstance AddOrUpdate(string resource, string serviceUriString, DateTime registrationExperation);
    }
}
