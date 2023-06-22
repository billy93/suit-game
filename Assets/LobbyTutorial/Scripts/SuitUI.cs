using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class SuitUI : MonoBehaviour
{
    [SerializeField] private Button gunting;
    [SerializeField] private Button batu;
    [SerializeField] private Button kertas;
    
    // Start is called before the first frame update
    void Start()
    {
        gunting.onClick.AddListener(() => {
           SuitGameManager.Instance.ChooseServerRPC((int)SuitGameManager.Selection.Scissor);
        });
        batu.onClick.AddListener(() => {
           SuitGameManager.Instance.ChooseServerRPC((int)SuitGameManager.Selection.Rock);
        });
        kertas.onClick.AddListener(() => {
           SuitGameManager.Instance.ChooseServerRPC((int)SuitGameManager.Selection.Paper);            
        });

        SuitGameManager.Instance.OnStateChanged += SuitGameManager_OnStateChanged;
        gameObject.SetActive(false);
    }

    private void SuitGameManager_OnStateChanged(object sender, System.EventArgs e) {
        if(SuitGameManager.Instance.IsGameStart()){
            gameObject.SetActive(true);
        }
        else if(SuitGameManager.Instance.IsGameEnd()){
            gameObject.SetActive(false);
            EndGameUI.Instance.Show();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
