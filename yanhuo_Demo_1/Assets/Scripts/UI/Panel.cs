using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Panel : MonoBehaviour
{
    public static Panel panel;
    public Button btn_bank;
    public Button btn_player1;
    public Button btn_player2;
    

    // Start is called before the first frame update
    void Start()
    {
        
        btn_bank.onClick.AddListener(() =>
        {
            Bank_Panel.bp.gameObject.SetActive(true);
        });
        btn_player1.onClick.AddListener(() =>
        {
            Player_Panel.pp.gameObject.SetActive(true);
        });
        btn_player2.onClick.AddListener(() =>
        {
            Player_Panel.pp.gameObject.SetActive(true);
        });

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
