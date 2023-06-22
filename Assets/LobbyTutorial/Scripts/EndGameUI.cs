using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class EndGameUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI status;
    [SerializeField] private Button mainMenu;

    public static EndGameUI Instance { get; private set; }

    private void Awake(){
        Instance = this;
        gameObject.SetActive(false);
        mainMenu.onClick.AddListener(() => {
            SuitGameManager.Instance.Shutdown();
            
            //Loader.Load(Loader.Scene.SuitMainMenuScene);
        });
       
    }

    public void Show(){
        if(SuitGameManager.Instance.IsGameEnd()){
            if (SuitGameManager.Instance.IsWinner() == 1)
            {
                status.text = "You WIN";
            }
            else if(SuitGameManager.Instance.IsWinner() == 0)
            {
                status.text = "You LOSE";
            }
            else{
                status.text = "DRAW!!!";
            }
        }    
        gameObject.SetActive(true);
    }
}
