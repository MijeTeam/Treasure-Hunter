using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FXParticle : MonoBehaviour
{
	public bool m_OnlyDeactivate = false;
	//private Coroutine m_kCoroutine = null;

	private bool m_bLoop = true;

	// Start is called before the first frame update
	void OnEnable()
	{
		StartCoroutine("EnumFunc_CheckAlive", 1f);
	}

	public void Show(bool bShow)
    {
		gameObject.SetActive(bShow);
    }

    public void Play()
    {
		Stop();
		Invoke("Callback_StartFX", 0.1f);
	}


	void Callback_StartFX()
    {
		Show(true);
	}


	public void Stop()
    {
		m_bLoop = false;
		Show(false);
	}


    IEnumerator EnumFunc_CheckAlive(float fDealy)
	{
		m_bLoop = true;

		while (m_bLoop)
		{
			yield return new WaitForSeconds(fDealy);
			if (!GetComponent<ParticleSystem>().IsAlive(true))
			{
				if (m_OnlyDeactivate)
					this.gameObject.SetActive(false);
				else
					GameObject.Destroy(this.gameObject);

				m_bLoop = false;
				break;
			}
		}
	}
}
