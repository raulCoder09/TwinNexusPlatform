using System;
using System.Threading.Tasks;

namespace _Scripts.Controllers.EnvironmentController
{
    /// <summary>
    /// Interface que define las operaciones de control de ambientes
    /// </summary>
    public interface IEnvironmentController
    {
        // Propiedades
        bool IsEnvironmentLoaded { get; }
        EnvironmentType CurrentEnvironment { get; }
        bool IsTransitioning { get; }
        
        // Métodos principales
        Task<bool> LoadEnvironment(EnvironmentType environmentType);
        void UnloadCurrentEnvironment();
        
        // Eventos
        event Action<EnvironmentType> OnEnvironmentLoaded;
        event Action<EnvironmentType> OnEnvironmentUnloaded;
        event Action<EnvironmentType, float> OnEnvironmentLoadProgress;
        event Action<string> OnEnvironmentError;
    }
    
    /// <summary>
    /// Tipos de ambientes disponibles
    /// </summary>
    public enum EnvironmentType
    {
        None,
        Virtual,
        AugmentedReality,
        Hybrid,
        RealDevice
    }
}