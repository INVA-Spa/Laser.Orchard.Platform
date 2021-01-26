﻿using Laser.Orchard.StartupConfig.Models;
using Orchard.Environment.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.StartupConfig.ViewModels {
    [OrchardFeature("Laser.Orchard.BearerTokenAuthentication")]
    public class ApiCredentialsPartViewModel {
        public ApiCredentialsPartViewModel(ApiCredentialsPart part) {

            Part = part;
        }
        public ApiCredentialsPart Part { get; set; }
        public string ApiKey { get; set; }
        public string ApiSecret { get; set; }

        public bool HasCredentials() {
            return !string.IsNullOrWhiteSpace(ApiKey)
                && !string.IsNullOrWhiteSpace(ApiSecret);
        }
    }
}