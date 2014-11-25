using System;
using System.Collections.Generic;

namespace service_discovery
{
    public interface IServiceRegistry
    {
        IEnumerable<ServiceInstance> GetServiceInstancesForResource(ServiceInstancesRequest request);
        RegistrationTicket Register(ServiceRegistration registration);
    }
}
