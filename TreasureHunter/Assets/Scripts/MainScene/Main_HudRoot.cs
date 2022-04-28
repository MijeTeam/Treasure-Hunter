using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main_HudRoot : MonoBehaviour
{
    public void GoTreasure() => TransitionFade.Inst.SceneLoad("TreasureScene");
    public void GoGameScene() => TransitionFade.Inst.SceneLoad("GameScene");

}
