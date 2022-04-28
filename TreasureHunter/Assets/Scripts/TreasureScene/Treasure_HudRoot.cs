using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Treasure_HudRoot : MonoBehaviour
{
    public void GoMainScene() => TransitionFade.Inst.SceneLoad("MainScene");

}
