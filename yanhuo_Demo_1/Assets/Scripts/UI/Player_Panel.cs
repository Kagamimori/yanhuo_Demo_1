using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player_Panel : MonoBehaviour//目标：这种面板随时可以打开，但只有在对的时间按钮才能响应
{
    public Button btn_cancel;
    public static Player_Panel pp;
    private void Awake()
    {
        pp = this;
        Player_Panel.pp.gameObject.SetActive(false);
    }
    
    // Start is called before the first frame update
    void Start()
    {
        btn_cancel.onClick.AddListener(() =>
        {
            Player_Panel.pp.gameObject.SetActive(false);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
