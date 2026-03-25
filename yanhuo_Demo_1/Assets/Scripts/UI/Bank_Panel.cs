using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Bank_Panel : MonoBehaviour
{
    public Button btn_cancel;
    public static Bank_Panel bp;
    private void Awake()
    {
        bp = this;
        Bank_Panel.bp.gameObject.SetActive(false);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        btn_cancel.onClick.AddListener(() =>
        {
            Bank_Panel.bp.gameObject.SetActive(false);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
