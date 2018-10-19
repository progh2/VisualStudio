﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace GitHub.Helpers
{
    /// <summary>
    /// This a workaround for extensions that define a ProvideBindingPath attribute and
    /// install for AllUsers.
    /// </summary>
    /// <remarks>
    /// Extensions that are installed for AllUsers, will also be installed for all
    /// instances of Visual Studio - including the experimental (Exp) instance which
    /// is used in development. This isn't a problem so long as all features that
    /// exist in the AllUsers extension, also exist in the extension that is being
    /// developed.
    /// 
    /// When an extension uses the ProvideBindingPath attribute, the binding path for
    /// the AllUsers extension gets installed at the same time as the one in development.
    /// This doesn't matter when an assembly is strong named and is loaded using its
    /// full name (including version number). When an assembly is loaded using its
    /// simple name, assemblies from the AllUsers extension can end up loaded at the
    /// same time as the extension being developed. This can happen when an assembly
    /// is loaded from XAML or an .imagemanifest.
    /// 
    /// This is a workaround for that issue. The <see cref="FindRedundantBindingPaths(List{string}, string)" />
    /// method will check to see if a reference assembly could be loaded from an alternative
    /// binding path. It will return any alternative paths that is finds.
    /// </remarks>
    public static class BindingPathUtilities
    {
        /// <summary>
        /// Find any alternative binding path that might have been installed by an AllUsers extension.
        /// </summary>
        /// <param name="bindingPaths">A list of binding paths to search</param>
        /// <param name="assemblyLocation">A reference assembly that has been loaded from the correct path.</param>
        /// <returns>A list of redundant binding paths.</returns>
        public static IList<string> FindRedundantBindingPaths(List<string> bindingPaths, string assemblyLocation)
        {
            var fileName = Path.GetFileName(assemblyLocation);
            return bindingPaths
                .Select(p => (path: p, file: Path.Combine(p, fileName)))
                .Where(pf => File.Exists(pf.file))
                .Where(pf => !pf.file.Equals(assemblyLocation, StringComparison.OrdinalIgnoreCase))
                .Select(pf => pf.path)
                .ToList();
        }

        /// <summary>
        /// Use reflection to find Visual Studio's list of binding paths.
        /// </summary>
        /// <returns>A live list of binding paths or an empty list if not running in Visual Studio.</returns>
        public static List<string> FindBindingPaths()
        {
            var manager = AppDomain.CurrentDomain.DomainManager;
            var property = manager?.GetType().GetProperty("BindingPaths", BindingFlags.NonPublic | BindingFlags.Instance);
            var bindingPaths = property?.GetValue(manager) as List<string>;
            return bindingPaths ?? new List<string>(0);
        }
    }
}
