using System;
using UnityEngine;

[Serializable]
public class GPUAnimationClip
{
    public AnimationClip aniclip;
    public int samplesPerSecond = 60;         //采样帧数
    public int StartFrame;                    //起始帧数
    public int EndFrame;                    //结束帧数
    public bool Loop;
    public string Name { get => aniclip.name; }
}