using DG.Tweening.Core.Easing;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UI.Inherited.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

namespace UI.Inherited
{
    public class BaseUI : UIBehaviour, Interface.UIObserver
    {
        [System.Serializable]
        public class InnerFlag
        {
            //** Active 가능 여부
            //** Hierarchy에 존재하면 1, Prefab형태로 생성해야하면 0
            public const int active = 0x0001;

            //** Overlap 가능 여부
            //** 단일로만 생성할 수 있으면 2, 복수생성이 가능하면 0
            public const int single = 0x0002;

            //** Bitflag 방식을 이용한 flag data
            public int has;

            //** Flag 소유 여부
            public bool Has(int flag)
            {
                if ((has & flag) == flag) return true;
                else return false;
            }

            //** string output about flag data
            public string GetHasString()
            {
                string makeString = string.Empty;
                if (Has(active)) makeString += "Active, ";
                if (Has(single)) makeString += "Single, ";

                if (makeString.CompareTo(string.Empty) != 0)
                {
                    int index = makeString.LastIndexOf(',');
                    makeString = makeString.Remove(index);
                }

                return makeString;
            }
        }

        [SerializeField] protected string id;
        [SerializeField] protected int hash;
        [SerializeField] protected int priority;
        [SerializeField] protected InnerFlag flag;
        [SerializeField] protected Subject subject;

        //== UI reference
        [SerializeField] protected Button closeButton;
        [SerializeField] protected Production production;

        private System.Action openCallback;
        private System.Action closeCallback;
        private System.Action forceCloseCallback;

        private Coroutine openProduction;
        private Coroutine closeProduction;


        #region Property list
        public string ID { get { return id; } }
        public int Hash { get { return hash; } }
        public int Priority { get { return priority; } }
        public InnerFlag Flag { get { return flag; } }
        public Button CloseButton { get { return closeButton; } }

        public bool IsOpenning
        {
            get
            {
                if(production == null) { return false; }
                else
                {
                    return production.IsOpenRunning;
                }
            }
        }
        public bool IsClosing
        {
            get
            {
                if(production == null) { return false;}
                else
                {
                    return production.IsCloseRunning;
                }
            }
        }
        #endregion End - Property list

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            ValidateMathod();
        }

        //== NOTE: override된 OnValidate는 호출되지 않아, 따로 제작하여 호출합니다.
        protected virtual void ValidateMathod()
        {
            if (closeButton == null)
            {
                GameObject obj = new GameObject("Close");
                obj.transform.SetParent(this.transform);
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;

                Image targetGraphic = obj.AddComponent<Image>();
                closeButton = obj.AddComponent<Button>();
                closeButton.targetGraphic = targetGraphic;
            }

            if (production == null)
            {
                Production production = GetComponent<Production>();
                if (production == null)
                {
                    production = gameObject.AddComponent<Production>();
                }

                this.production = production;
            }
        }
#endif
        //== NOTE: 비활성화로 시작하는 ui 오브젝트의 특성상 전처리 초기화를 위하여 선언.
        public virtual void FirstInit() { }

        public virtual void Init(float width, float height, int hash, Subject subject)
        {
            production.Init(width,height);

            this.hash = hash;
            this.subject = subject;
        }

        public void Open()
        {
            if (openProduction == null)
            {
                openProduction = StartCoroutine(OpenProgress());
            }
        }
        public void Close(bool distroy)
        {
            if (closeProduction == null)
            {
                closeProduction = StartCoroutine(CloseProgress(distroy));
            }
        }
        public void ForceClose(bool distroy ,System.Action closeCallback)
        {
            production.ForceCloseOn();
            forceCloseCallback = closeCallback;

            Close(distroy);
        }
        public void SetEvent(System.Action openCallback, System.Action closeCallback)
        {
            this.openCallback = openCallback;
            this.closeCallback = closeCallback;
        }

        IEnumerator OpenProgress()
        {
            if (production.IsOpenRunning == true) yield break;

            yield return null;
            production.CallOpenProcessing();

            //== 애니메이션 시작을 위해 대기
            yield return new WaitUntil(() => production.IsOpenRunning == true);
            yield return new WaitUntil(() => production.IsOpenRunning == false);

            openCallback?.Invoke();
            openCallback = null;

            subject.Register(hash);
            openProduction = null;
        }
        IEnumerator CloseProgress(bool destroy)
        {
            if (production.IsCloseRunning == true) yield break;

            yield return null;
            production.CallCloseProduction();

            //== 애니메이션 시작을 위해 대기
            yield return new WaitUntil(() => production.IsCloseRunning == true);
            yield return new WaitUntil(() => production.IsCloseRunning == false);

            closeCallback?.Invoke();
            closeCallback = null;

            subject.UnRegister(hash);

            if (destroy) { Destroy(gameObject); }
            else
            {
                gameObject.SetActive(false);
            }
            if(production.ForceCloseFlag)
            {
                forceCloseCallback?.Invoke();
                forceCloseCallback = null;

                production.ForceCloseOff();
            }

            closeProduction = null;
        }

        public virtual string GetState()
        {
            return $"[ ID\t: {id} ]\n" +
                    $"[ Hash\t\t: {hash} ]\n" +
                    $"[ Priority\t: {priority} ]" +
                    $"[ Flag\t\t: {flag.GetHasString()} ]";
        }

        public void ShowState()
        {
            Debug.Log(GetState());
        }
    }
}
