﻿using Orchard.Environment.Extensions;
using Orchard.Mvc.Routes;
using Orchard.WebApi.Routes;
using System.Collections.Generic;

namespace Laser.Orchard.WebServices.Routes {
    [OrchardFeature("Laser.Orchard.CustomRestApi")]
    public class CustomRESTApiRoutes : IHttpRouteProvider {
        public IEnumerable<RouteDescriptor> GetRoutes() {
            yield return (
                new HttpRouteDescriptor {
                    // Aliases formed by Autoroute have Priority 80
                    Priority = 85,
                    RouteTemplate = "API/REST/{actionName}",
                    Defaults = new {
                        area = "Laser.Orchard.WebServices",
                        controller = "CustomRESTApi"
                    }
                }
            );
        }

        public void GetRoutes(ICollection<RouteDescriptor> routes) {
            foreach (var routeDescriptor in GetRoutes()) {
                routes.Add(routeDescriptor);
            }
        }
    }
}