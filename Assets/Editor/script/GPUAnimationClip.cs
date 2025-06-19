using System;
using UnityEngine;

[Serializable]
public class GPUAnimationClip
{
    public AnimationClip aniclip;
    public int samplesPerSecond = 60;         //����֡��
    public int StartFrame;                    //��ʼ֡��
    public int EndFrame;                    //����֡��
    public bool Loop;
    public string Name { get => aniclip.name; }
}