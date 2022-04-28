using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TransitionFade : MonoBehaviour
{
    [SerializeField] Image black;
    [SerializeField] Canvas canvas;

    private static TransitionFade inst;
    private static bool alive = true;

    private bool bTransition = false;

    public static TransitionFade Inst
    {
        get
        {
            // 앱이 꺼젔거나 Destroy됬는지 체크
            if (!alive)
            {
                Debug.LogWarning(typeof(TransitionFade) + "' is already destroyed on application quit.");
                return null;
            }

            //C# 2.0 Null 병합연산자
            return inst ?? FindObjectOfType<TransitionFade>();
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();
    }

    void OnApplicationExit()
    {
        alive = false;
    }

    void Initialise()
    {
        black.color = new Color(0, 0, 0, 0);
        gameObject.name = "TransitionManager";
        DontDestroyOnLoad(this.gameObject);
    }

    void Awake()
    {
        if (inst == null)
        {
            inst = this;
            Initialise();
        }
        else if (inst != this)
        {
            Destroy(this.gameObject);
        }
    }


    public void SceneLoad(string name, float firstdelay = 0.0f, float middledelay = 0.0f, float amount = 1.0f, bool white = false)
    {
        if (!bTransition)
            StartCoroutine(Enum_Scene(name, firstdelay, middledelay, amount, white));
    }

    IEnumerator Enum_Scene(string name, float firstdelay, float middledelay, float amount, bool white)
    {
        yield return new WaitForSeconds(firstdelay);

        //Color col = new Color(white ? 1 : 0, white ? 1 : 0, white ? 1 : 0, 0);
        black.gameObject.SetActive(true);
        black.GetComponent<Animator>().Play(white ? "WhiteFadeIn" : "BlackFadeIn");
        black.GetComponent<Animator>().speed = amount;
        bTransition = true;

        //float alpha = 0;
        //while(alpha < 1)
        //{
        //    alpha += Time.deltaTime * amount;
        //    black.color = new Color(col.r, col.g, col.b, alpha);
        //    yield return new WaitForSeconds(Time.deltaTime);
        //}

        yield return new WaitForSeconds(1 / amount);

        SceneManager.LoadScene(name);

        yield return new WaitForSeconds(Time.deltaTime + middledelay);
        canvas.worldCamera = Camera.main; // 다음 프레임에 메인카메라의 정보가 바뀌므로 현재의 캔버스 정보를 메인카메라로 덮어줌

        black.GetComponent<Animator>().Play(white ? "WhiteFadeOut" : "BlackFadeOut");
        //alpha = 1;
        //while (alpha > 0)
        //{
        //    alpha -= Time.deltaTime * amount;
        //    black.color = new Color(col.r, col.g, col.b, alpha);
        //    yield return new WaitForSeconds(Time.deltaTime);
        //}
        yield return new WaitForSeconds(1 / amount);

        black.gameObject.SetActive(false);
        bTransition = false;
        yield return null;
    }    

    void Update()
    {
        
    }
}
