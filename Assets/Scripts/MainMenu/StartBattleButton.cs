using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StartBattleButton : MonoBehaviour
{
    public void OnStart()
    {
        if (DeckManager.Instance.selectedDeck.Count == 8)
        {
            SceneManager.LoadScene("CinematicLoading");
        }
        else
        {
            Debug.Log("����� ������� ����� 8 ����.");
        }
    }
}
