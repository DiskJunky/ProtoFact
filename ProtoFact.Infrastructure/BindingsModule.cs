using Ninject.Modules;
using ProtoFact.Abstractions;
using ProtoFact.Engine;

namespace ProtoFact.Infrastructure
{
    /// <summary>
    /// Configures dependency injection bindings.
    /// </summary>
    public class BindingsModule : NinjectModule
    {
        public override void Load()
        {
            // Core
            Bind<IInventory>().To<Inventory>().InSingletonScope();
            Bind<IEngine>().To<Engine.Engine>().InSingletonScope();
            Bind<IRateSolver>().To<RateSolver>().InSingletonScope();

            // Logging
            Bind<ILogger>().To<NLogLogger>().InSingletonScope();
        }
    }
}