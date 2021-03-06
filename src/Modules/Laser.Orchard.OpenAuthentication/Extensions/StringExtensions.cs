﻿using System.Text.RegularExpressions;
using Orchard.Users.Models;

namespace Laser.Orchard.OpenAuthentication.Extensions {
    public static class StringExtensions {
        public static bool IsEmailAddress(this string value) {
            return Regex.IsMatch(value, UserPart.EmailPattern, RegexOptions.IgnoreCase);
        }
    }
}