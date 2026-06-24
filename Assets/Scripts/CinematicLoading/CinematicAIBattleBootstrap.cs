using UnityEngine;

// В BattleMatch ArenaPlacementEvents.IsArenaPlaced выставляет игрок, разместив
// AR-арену. В кинематике этого шага нет, а SmartAIOpponent (не трогаем!) ждёт
// именно этот флаг перед стартом. Подтверждаем арену сразу и сбрасываем флаг
// при выходе из сцены, чтобы он не "утёк" в следующую BattleMatch и не сломал
// там реальную проверку размещения арены игроком.
public class CinematicAIBattleBootstrap : MonoBehaviour
{
    void Start()
    {
        ArenaPlacementEvents.InvokeArenaConfirmed();
    }

    void OnDestroy()
    {
        ArenaPlacementEvents.Reset();
    }
}
