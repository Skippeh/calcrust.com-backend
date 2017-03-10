using System;
using System.Reflection;
using Oxide.Core;
using Oxide.Core.Extensions;
using Oxide.Core.Plugins;

namespace RustCalc.Oxide
{
    public class RustCalcExtension : Extension
    {
        private readonly Version assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        public override string Name => "Rust Calculator Export Extension";
        public override string Author => "https://github.com/Skippeh/calcrust.com-backend";
        public override VersionNumber Version => new VersionNumber(assemblyVersion.Major, assemblyVersion.Minor, assemblyVersion.Build);
        
        public RustCalcExtension(ExtensionManager manager) : base(manager)
        {
            if (!Interface.Oxide.RootPluginManager.AddPlugin(new RustCalcPlugin()))
            {
                Interface.Oxide.LogError("Could not add RustCalc plugin.");
            }
        }
    }
}