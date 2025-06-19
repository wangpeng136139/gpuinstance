using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GPUAnimationEventManager
{
    [Serializable]
    public class GPUAnimaitonEvent
    {
        public string animationName;
        public UnityEvent OnAniBeginEvent = new UnityEvent();
        public UnityEvent OnAniUpdateEvent = new UnityEvent();
        public UnityEvent OnAniEndEvent = new UnityEvent();

        public GPUAnimaitonEvent(string animationName)
        {
            this.animationName = animationName;
        }
    }

    public GPUAnimationEventManager(GPUAnimation gPUAnimation)
    {
        this.gPUAnimation = gPUAnimation;
        List<GPUAnimationClip> clips = gPUAnimation.GetAllClip();
        for (int i = 0; i < clips.Count; i++)
        {
            if (!ContainEvent(clips[i].Name))
            {
                gPUAnimaitonEvents.Add(new GPUAnimaitonEvent(clips[i].Name));
            }
        }
    }

    private GPUAnimation gPUAnimation;

    [SerializeField] private List<GPUAnimaitonEvent> gPUAnimaitonEvents = new List<GPUAnimaitonEvent>();

    public bool ContainEvent(string animationName)
    {
        for (int i = 0; i < gPUAnimaitonEvents.Count; i++)
        {
            if (gPUAnimaitonEvents[i].animationName == animationName)
                return true;
        }
        return false;
    }

    public void AddAniBeginEvent(string animationName, UnityAction unityAction)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }

        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniBeginEvent.AddListener(unityAction);
                return;
            }
        }
    }

    public void AddAniUpdateEvent(string animationName, UnityAction unityAction)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniUpdateEvent.AddListener(unityAction);
                return;
            }
        }
    }


    public void AddAniEndEvent(string animationName, UnityAction unityAction)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniEndEvent.AddListener(unityAction);
                return;
            }
        }
    }

    public void RemoveAniBeginEvent(string animationName, UnityAction unityAction)
    {
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniBeginEvent.RemoveListener(unityAction);
                return;
            }
        }
    }

    public void RemoveAniUpdateEvent(string animationName, UnityAction unityAction)
    {
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniUpdateEvent.RemoveListener(unityAction);
                return;
            }
        }
    }

    public void RemoveAniEndEvent(string animationName, UnityAction unityAction)
    {
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniEndEvent.RemoveListener(unityAction);
                return;
            }
        }
    }

    public void InvokeAniBeginEvent(string animationName)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniBeginEvent?.Invoke();
                return;
            }
        }
    }


    public void InvokeAniUpdateEvent(string animationName)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniUpdateEvent?.Invoke();
                return;
            }
        }
    }

    public void InvokeAniEndEvent(string animationName)
    {
        if (!gPUAnimation.ContainClip(animationName))
        {
            Debug.LogError($"{gPUAnimation.gameObject}没有{animationName}动画");
            return;
        }
        foreach (var aniEvent in gPUAnimaitonEvents)
        {
            if (animationName == aniEvent.animationName)
            {
                aniEvent.OnAniEndEvent?.Invoke();
                return;
            }
        }
    }
}