using System.Collections.Generic;
using UnityEngine;

public class GPUAnimationData : ScriptableObject
{
    public int totalFrame = 0;        //��֡��
    public List<GPUAnimationClip> clips = new List<GPUAnimationClip>();
    public Dictionary<string, GPUAnimationClip> gpudic;

    // �ڼ��ػ��ʼ��ʱ�ؽ��ֵ�
    public void Init()
    {
        gpudic = new Dictionary<string, GPUAnimationClip>();
        foreach (var clip in clips)
        {
            gpudic.Add(clip.Name, clip);
            gpudic[clip.Name] = clip;
        }
    }
    public void AddClips(GPUAnimationClip clip)
    {
        clips.Add(clip);
    }
    public bool ContainClip(string name) => gpudic.ContainsKey(name);
    public GPUAnimationClip GetClip(string name) => gpudic[name];
    public float GetStartFrame(string name) => gpudic[name].StartFrame;
    public float GetEndFrame(string name) => gpudic[name].EndFrame;
}