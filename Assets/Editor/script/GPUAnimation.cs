using System.Collections.Generic;
using UnityEngine;

public class GPUAnimation : MonoBehaviour
{
    public MeshRenderer mesh;
    public GPUAnimationData data;

    private float currentFrame = 0;
    private float baseRate = 60;
    private float speed = 1;                            //������������

    private int totalFrames { get => data.totalFrame; }
    private GPUAnimationClip currentClip;               //��ǰ���ŵĶ���
    private MaterialPropertyBlock block;

    [SerializeField] private GPUAnimationEventManager eventManager;
    public GPUAnimationEventManager EventManager { get { return eventManager; } }

    private void Awake()
    {
        eventManager = new GPUAnimationEventManager(this);
    }

    private void Start()
    {
        block = new MaterialPropertyBlock();
        data.Init();
    }

    private void Update()
    {
        InvokeAnimation();
        mesh.SetPropertyBlock(block);
    }

    public void Play(string name, bool loop = true)
    {
        if (!data.ContainClip(name))
        {
            Debug.LogError($"{this.gameObject.name}������{name}����");
            return;
        }
        currentClip = data.GetClip(name);
        currentClip.Loop = loop;
        currentFrame = currentClip.StartFrame;
        eventManager.InvokeAniBeginEvent(name);
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    /// <summary>
    /// �Ƿ��������Ƭ��
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool ContainClip(string name)
    {
        return data.ContainClip(name);
    }

    public List<GPUAnimationClip> GetAllClip() => data.clips;

    /// <summary>
    /// ִ�ж���Ƭ��
    /// </summary>
    private void InvokeAnimation()
    {
        if (currentClip == null)
            return;
        currentFrame += Time.deltaTime * baseRate * speed;
        eventManager.InvokeAniUpdateEvent(currentClip.Name);
        if (currentFrame >= currentClip.EndFrame)
        {
            eventManager.InvokeAniEndEvent(currentClip.Name);
            if (currentClip.Loop)
            {
                eventManager.InvokeAniBeginEvent(currentClip.Name);
                currentFrame = currentClip.StartFrame;
            }
            else
                currentFrame = currentClip.EndFrame;
        }

        block.SetFloat("_TimeOffset", currentFrame / totalFrames);
    }
}