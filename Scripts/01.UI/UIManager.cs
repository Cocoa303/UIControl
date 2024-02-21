using System.Collections;
using System.Collections.Generic;
using System.Text;
using UI.Inherited.Interface;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Jobs;
using UnityEngine.UI;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;
using BaseUI = UI.Inherited.BaseUI;

namespace UI
{
    public partial class UIManager : Util.Inherited.DisposableSingleton<UIManager>, UI.Inherited.Interface.Subject
    {
        public enum eOpenState
        {
            SuccessOpen,            //== 성공적인 오픈
            ImpossibleOverlap,      //== 중복오픈 불가능
            NotFind                 //== UI 객체를 찾을수가 없음.
        }
    }

    public partial class UIManager : Util.Inherited.DisposableSingleton<UIManager>, UI.Inherited.Interface.Subject
    {
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] RectTransform uiParent;

        //== UI 백그라운드 클릭을 막기위한 커버
        [SerializeField] RectTransform backgroundCover;

        [SerializeField] private List<BaseUI> database;
        [SerializeField, ReadOnly] private List<BaseUI> opens;
        [SerializeField, ReadOnly] private List<int> openHashs;

        [SerializeField, ReadOnly] private eOpenState openState;

        //== 백스페이스(뒤로가기) 활성화를 위한 ui control 변수들
        [SerializeField] bool stackable = true;
        [SerializeField] int stackSize = 10;
        [SerializeField, ReadOnly] private Queue<KeyValuePair<string /* ID */, int /* Hash */>> closeStack;

        #region Property list
        public eOpenState OpenState { get => openState; }
        public bool LeastOneOpen
        {
            get
            {
                if (opens == null) return false;
                if (opens.Count == 0) return false;

                return true;
            }
        }
        #endregion End - Property list

        [System.Obsolete]
        private void OnValidate()
        {
            #region Create
            //== UI의 Main Canvas 생성 [ 초기 1회 ]
            if (mainCanvas == null)
            {
                //== Canvas Create
                GameObject canvas = new GameObject("UI Main canvas");
                mainCanvas = canvas.AddComponent<Canvas>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.pixelPerfect = false;
                mainCanvas.sortingOrder = 0;
                mainCanvas.targetDisplay = 0;
                mainCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

                //== Canvas scaler set
                CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                scaler.referencePixelsPerUnit = 100;

                //== Graphic raycaster set
                GraphicRaycaster raycaster = canvas.AddComponent<GraphicRaycaster>();
                raycaster.ignoreReversedGraphics = true;
                raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;

                //== Transform set
                canvas.transform.SetParent(this.transform);
            }

            if (backgroundCover == null)
            {
                GameObject uiBackgroundCover = new GameObject("Background Cover", typeof(RectTransform));

                //** Color Setting **//
                Image cover = uiBackgroundCover.AddComponent<Image>();
                cover.color = new Color(0, 0, 0, 20.0f / 255.0f);
                cover.raycastTarget = true;

                cover.transform.SetParent(mainCanvas.transform);
                cover.transform.localScale = Vector3.one;
                cover.rectTransform.anchorMin = Vector3.zero;
                cover.rectTransform.anchorMax = Vector3.one;
                cover.rectTransform.offsetMin = Vector3.zero;
                cover.rectTransform.offsetMax = Vector3.zero;

                backgroundCover = cover.rectTransform;
                backgroundCover.gameObject.SetActive(false);
            }

            if (uiParent == null)
            {
                GameObject parent = new GameObject("UI Items", typeof(RectTransform));
                parent.transform.SetParent(mainCanvas.transform);
                uiParent = parent.transform as RectTransform;

                uiParent.localScale = Vector3.one;
                uiParent.anchorMin = Vector3.zero;
                uiParent.anchorMax = Vector3.one;
                uiParent.offsetMin = Vector3.zero;
                uiParent.offsetMax = Vector3.zero;
            }

            EventSystem findEventSystem = GameObject.FindObjectOfType<EventSystem>(true);
            if (findEventSystem == null)
            {
                GameObject eventSystem = new GameObject("Event System");
                eventSystem.transform.SetParent(null);
                eventSystem.transform.localPosition = Vector3.zero;
                eventSystem.transform.localScale = Vector3.one;
                EventSystem system = eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }
            #endregion

#if UNITY_EDITOR
            //== Database flag option setting
            Object caching = UnityEditor.Selection.activeObject;
            if (database != null && database.Count > 0)
            {
                for (int i = 0; i < database.Count; i++)
                {
                    UnityEditor.Selection.activeObject = database[i];

                    //== Prefab은 Transform이 active 처리되지 않아,
                    //== 프리팹 구분하기 위한 방법으로 사용
                    if (UnityEditor.Selection.activeTransform != null)
                    {
                        database[i].Flag.has |= BaseUI.InnerFlag.active;
                        database[i].Flag.has |= BaseUI.InnerFlag.single;
                    }
                    else
                    {
                        database[i].Flag.has &= ~BaseUI.InnerFlag.active;
                    }
                }
            }
            UnityEditor.Selection.activeObject = caching;
#endif
        }

        private void Start()
        {
            opens = new List<BaseUI>();
            closeStack = new Queue<KeyValuePair<string, int>>();

            for (int i = 0; i < database.Count; i++)
            {
                if (database[i].Flag.Has(BaseUI.InnerFlag.active))
                {
                    database[i].FirstInit();
                }
            }
        }

        private void RemoveClosed()
        {
            if (stackSize <= closeStack.Count)
            {
                for (int i = 0; i <= closeStack.Count - stackSize; i++)
                {
                    closeStack.Dequeue();
                }
            }
        }
        private void InsertClosed(BaseUI ui)
        {
            if (stackable == false) return;

            closeStack.Enqueue(new KeyValuePair<string, int>(ui.ID, ui.Hash));

            RemoveClosed();
        }
        private void InsertClosed(List<BaseUI> uis, bool reverse = true)
        {
            if (stackable == false) return;

            if (reverse)
            {
                for (int i = uis.Count - 1; i >= 0; i--)
                {
                    closeStack.Enqueue(new KeyValuePair<string, int>(uis[i].ID, uis[i].Hash));
                }
            }
            else
            {
                for (int i = 0; i < uis.Count; i++)
                {
                    closeStack.Enqueue(new KeyValuePair<string, int>(uis[i].ID, uis[i].Hash));
                }
            }

            RemoveClosed();
        }

        private BaseUI UIControl(string uiID, bool coverAble = true, BaseUI.UICallback openCallback = null, BaseUI.UICallback closeCallback = null)
        {
            BaseUI baseUI = database.Find((ui) => ui.ID.CompareTo(uiID) == 0);
            if (baseUI == null)
            {
                openState = eOpenState.NotFind;
                Debug.Log($"Not find ui component [{uiID}]");
                return null;
            }

            //== 종료 애니메이션 진행중일때, 강제종료후 새로 활성화 처리.
            //== ui를 닫았다 열었다 빠르게 진행될때 안열리는 현상을 방지하기 위해 처리.
            if (baseUI.Flag.Has(BaseUI.InnerFlag.active) && baseUI.IsClosing)
            {
                Debug.Log("Is Close running : Invoke open running");
                baseUI.ForceClose(!baseUI.Flag.Has(BaseUI.InnerFlag.active), (_) =>
                {
                    Open(uiID, coverAble, openCallback, closeCallback);
                });
                return null;
            }

            if (baseUI.Flag.Has(BaseUI.InnerFlag.single))
            {
                if (opens.Find((ui) => ui.ID.CompareTo(baseUI.ID) == 0))
                {
                    if (baseUI.Flag.Has(BaseUI.InnerFlag.active) && baseUI.gameObject.activeSelf == false)
                    {
                        opens.RemoveAll((v) => v == null || v.gameObject == null || v.gameObject.activeSelf == false);
                    }
                    else
                    {
                        Debug.Log("That ui is single object.");
                        openState = eOpenState.ImpossibleOverlap;
                        return null;
                    }
                }
            }

            BaseUI ui = null;
            if (baseUI.Flag.Has(BaseUI.InnerFlag.active))
            {
                baseUI.gameObject.SetActive(true);
                ui = baseUI;
            }
            else
            {
                ui = Instantiate(baseUI);
                ui.gameObject.name = "Clone : " + ui.ID + "[ Hash : " + ui.Hash + " ]";

                //** Hierarchy transform set **//
                ui.transform.SetParent(uiParent.transform);
                ui.transform.localPosition = Vector3.zero;
                ui.transform.localScale = Vector3.one;
                ui.transform.rotation = Quaternion.identity;
            }

            //== Set priority
            List<BaseUI> priority = opens.FindAll((v) => v.Priority <= ui.Priority);

            if (priority != null)
            {
                int indexer = priority.Count;
                ui.transform.SetSiblingIndex(indexer);
            }
            else
            {
                ui.transform.SetAsFirstSibling();
            }

            ui.CloseButton.onClick.RemoveAllListeners();
            ui.CloseButton.onClick.AddListener(() => ui.Close(!ui.Flag.Has(BaseUI.InnerFlag.active)));

            ui.SetEvent(openCallback, closeCallback);

            openState = eOpenState.SuccessOpen;

            return ui;
        }

        public BaseUI Open(string uiID, bool coverAble = true, BaseUI.UICallback openCallback = null, BaseUI.UICallback closeCallback = null)
        {
            BaseUI ui = UIControl(uiID, coverAble, openCallback, closeCallback);

            if (ui != null)
            {
                ui.Init(mainCanvas.pixelRect.width, mainCanvas.pixelRect.height,
                    ui.GetHashCode(), this);

                if(coverAble)
                {
                    backgroundCover.gameObject.SetActive(true);
                }
                ui.Open();

                return ui;
            }
            else
            {
                return null;
            }
        }

        public bool CloseAll()
        {
            if (opens != null && opens.Count != 0)
            {
                InsertClosed(opens);
                
                for (int i = opens.Count - 1; 0 <= i; i--)
                {
                    opens[i].Close(!opens[i].Flag.Has(BaseUI.InnerFlag.active));
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        public bool LastClose()
        {
            if (1 <= opens.Count)
            {
                BaseUI ui = opens[opens.Count - 1];
                ui.Close(!ui.Flag.Has(BaseUI.InnerFlag.active));

                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetUIID(System.Type type)
        {
            for (int i = 0; i < database.Count; i++)
            {
                if (database[i].GetType() == type)
                {
                    return database[i].ID;
                }
            }

            return string.Empty;
        }

        public string GetPrevID()
        {
            if (2 <= opens.Count)
            {
                return opens[opens.Count - 2].ID;
            }
            else
            {
                return string.Empty;
            }
        }
        public string GetLastID()
        {
            if (1 <= opens.Count)
            {
                return opens[opens.Count - 1].ID;
            }
            else
            {
                return string.Empty;
            }
        }
        public List<string> GetOpenIDs()
        {
            if (opens.Count != 0)
            {
                List<string> revalue = new List<string>();

                for (int i = 0; i < opens.Count; i++)
                {
                    revalue.Add(opens[i].ID);
                }

                return revalue;
            }
            else
            {
                return null;
            }
        }

        public void CallObservers(System.Action<UIObserver> action)
        {
            for(int i = 0; i < opens.Count; i++)
            {
                action(opens[i]);
            }
        }

        public string GetState()
        {
            StringBuilder state = new StringBuilder();

            //== DB make
            if (database != null && database.Count != 0)
            {
                state.Append("[ UI in Database ]");
                for (int i = 0; i < database.Count; i++)
                {
                    state.Append('\n' + database[i].GetState());
                }
            }

            //== Open list make
            if (opens != null && opens.Count != 0)
            {
                state.Append("\n" + "[ UI in open list ]");
                for(int i = 0; i < opens.Count;i++)
                {
                    state.Append('\n' + opens[i].GetState());
                }
            }

            return state.ToString();
        }

        public void ShowState()
        {
            Debug.Log(GetState());
        }

        public void Register(UIObserver observer)
        {
            if (observer == null || observer is BaseUI == false) return;
            BaseUI ui = observer as BaseUI;

            opens.Add(ui);
            openHashs.Add(ui.Hash);
        }

        public void UnRegister(UIObserver observer)
        {
            if (observer == null || observer is BaseUI == false) return;

            BaseUI ui = observer as BaseUI;

            InsertClosed(ui);

            opens.Remove(ui);
            openHashs.Remove(ui.Hash);

            if (!LeastOneOpen)
            {
                backgroundCover.gameObject.SetActive(false);
            }
        }
    }
}
