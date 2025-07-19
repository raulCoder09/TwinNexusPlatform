namespace _Scripts.Controllers.ArScaraRobotController
{
    public interface IArScaraRobotOps
    {
        // Enciende los motores del robot
        void TurnMotorsOn();
        
        // Apaga los motores del robot
        void TurnMotorsOff();
        
        // Consulta el estado actual de los motores
        bool AreMotorsOn { get; }
        
        // Verifica si el robot está instanciado y listo
        bool IsRobotInstantiated { get; }
    }
}