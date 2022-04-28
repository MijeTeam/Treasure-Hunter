using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    Vector3 originPos;

    Coroutine enum_shake = null;

    float Lerp(float start, float end, float amount)
    {
        return start + (end - start) * amount;
    }

    public void Shake(float _amount, float _duration)
    {
        if (enum_shake != null)
            StopCoroutine(enum_shake);
        enum_shake = StartCoroutine(EnumShake(_amount, _duration));
    }

    public IEnumerator EnumShake(float _amount, float _duration)
    {
        originPos = transform.position;
        float timer = 0;
        while (timer <= _duration)  
        {
            transform.position = originPos + new Vector3(Random.Range(-_amount, _amount), Random.Range(-_amount, _amount), 0);
            timer += Time.deltaTime;
            _amount = Lerp(_amount, 0, Time.deltaTime * 3);
            yield return null;
        }
        transform.position = originPos;
    }
}
