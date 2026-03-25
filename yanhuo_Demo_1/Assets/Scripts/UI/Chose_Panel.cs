using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class Chose_Panel : MonoBehaviour
{
    public Button btn_cancel;
    public static Chose_Panel cp;
    private void Awake()
    {
        cp = this;
        Chose_Panel.cp.gameObject.SetActive(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        btn_cancel.onClick.AddListener(() =>
        {
            Chose_Panel.cp.gameObject.SetActive(false);
        });
    }

    // Update is called once per frame
    void Update()
    {

    }
}
