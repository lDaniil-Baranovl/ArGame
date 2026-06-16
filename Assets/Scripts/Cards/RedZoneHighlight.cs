using UnityEngine;

/// Поставь этот скрипт на RedMask1 (и любые другие запретные зоны).
/// Renderer объекта должен использовать материал RedZone.
/// Объект должен быть изначально невидим: отключи Renderer в инспекторе.
public class RedZoneHighlight : MonoBehaviour
{
    private Renderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    void OnEnable()
    {
        CardDragXR.OnUnitCardHeld += Show;
        CardDragXR.OnUnitCardReleased += Hide;
    }

    void OnDisable()
    {
        CardDragXR.OnUnitCardHeld -= Show;
        CardDragXR.OnUnitCardReleased -= Hide;
    }

    private void Show() { if (_renderer != null) _renderer.enabled = true; }
    private void Hide() { if (_renderer != null) _renderer.enabled = false; }
}
