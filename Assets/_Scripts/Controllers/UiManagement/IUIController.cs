using System;

namespace _Scripts.Controllers.UiManagement
{
    /// <summary>
    /// Interface base para todos los controladores de UI del sistema
    /// Define el contrato común que deben implementar todos los controladores
    /// </summary>
    public interface IUIController
    {
        #region Properties

        /// <summary>
        /// Indica si este controlador requiere autenticación para funcionar
        /// Welcome = false, Dashboard/Training/Operations/Reports = true
        /// </summary>
        bool RequiresAuthentication { get; }

        /// <summary>
        /// Indica si el controlador ha sido inicializado correctamente
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// Indica si el controlador está actualmente visible/activo
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Nombre único del controlador (para logging y debugging)
        /// </summary>
        string ControllerName { get; }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// Inicializa el controlador con sus dependencias
        /// Se ejecuta una sola vez durante la creación del controlador
        /// </summary>
        /// <returns>True si la inicialización fue exitosa</returns>
        bool Initialize();

        /// <summary>
        /// Muestra/activa la UI del controlador
        /// Solo se puede ejecutar si RequiresAuthentication = false O si el usuario está autenticado
        /// </summary>
        void Show();

        /// <summary>
        /// Oculta/desactiva la UI del controlador
        /// </summary>
        void Hide();

        /// <summary>
        /// Libera recursos y limpia el controlador
        /// Se ejecuta al destruir el controlador o cambiar de escena
        /// </summary>
        void Cleanup();

        #endregion

        #region Events

        /// <summary>
        /// Se dispara cuando el controlador se inicializa correctamente
        /// </summary>
        event Action<IUIController> OnControllerInitialized;

        /// <summary>
        /// Se dispara cuando el controlador se muestra/activa
        /// </summary>
        event Action<IUIController> OnControllerShown;

        /// <summary>
        /// Se dispara cuando el controlador se oculta/desactiva
        /// </summary>
        event Action<IUIController> OnControllerHidden;

        /// <summary>
        /// Se dispara cuando ocurre un error en el controlador
        /// </summary>
        event Action<IUIController, string> OnControllerError;

        #endregion
    }
}