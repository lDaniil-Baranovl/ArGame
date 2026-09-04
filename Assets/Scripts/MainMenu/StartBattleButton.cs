using UnityEngine;
using UnityEngine.UI;

public class StartBattleButton : MonoBehaviour
{
    public void OnStart()
    {
        if (DeckManager.Instance.selectedDeck.Count == 8)
        {
            ScreenFader.LoadScene("BattleMatch");
        }
        else
        {
            Debug.Log("����� ������� ����� 8 ����.");
        }
    }
}
