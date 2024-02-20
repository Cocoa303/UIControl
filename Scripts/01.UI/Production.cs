using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UI.Inherited
{
    public class Production : MonoBehaviour
    {
        public enum CallType
        {
            OnOpen,
            OnClick,
            OnClose,
        }
        public enum AnimationType
        {
            Punch,
            Rotation,
            ScrollUp,
            ScrollDown,
            ScrollDrop,
            ComeOut,
            ComeIn,
            EnterFromLeft,
            EnterFromRight,
            OutToLeft,
            OutToRight,
        }

        [System.Serializable]
        public class Information
        {
            public RectTransform target;
            public CallType callType;
            public AnimationType animationType;
            public bool fixInitialTransform;

            //= Animation reference
            [Header("Animation reference")]
            [ReadOnly] public Vector3 initialPosition;
            [ReadOnly] public Vector3 initialScale;
            public Tweener hasTween;

            //= Detailed settings
            [Header("Detail set")]
#if UNITY_EDITOR
            public bool autoSetDuration = true;
#endif
            public float duration = -1f;

            public Information Clone()
            {
                Information inforamtion = new Information();

                inforamtion.target = target;
                inforamtion.callType = callType;
                inforamtion.animationType = animationType;

                inforamtion.initialPosition = initialPosition;
                inforamtion.initialScale = initialScale;

                inforamtion.duration = duration;

                return inforamtion;
            }
        }


        #region Inner members
        [SerializeField, ArrayTitle("target")]
        List<Information> infomations;
        [SerializeField] List<UIBehaviour> enabledControls;

        //== 애니메이션 진행중 중복 실행를 막기위한 플래그
        [SerializeField] bool isOpenRunning;
        [SerializeField] bool isCloseRunning;

        //== 애니메이션 진행 여부 상관없이 강제종료를 위한 플래그
        [SerializeField] bool forceCloseFlag;

        [SerializeField, ReadOnly] float width;
        [SerializeField, ReadOnly] float height;

        Coroutine closeProduction;
        Coroutine openningProduction;
        #endregion End - Inner members

        #region Property list
        public bool IsOpenRunning { get => isOpenRunning; }
        public bool IsCloseRunning { get => isCloseRunning; }
        public bool ForceCloseFlag { get => forceCloseFlag; }

        #endregion End - Property list

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;

            //= 버튼 이벤트의 타겟이 버튼이 아닐경우 삽입 불가하게 설정
            if (Selection.activeGameObject == this.gameObject)
            {
                if (infomations != null)
                {
                    for (int i = 0; i < infomations.Count; i++)
                    {
                        if (infomations[i].callType == CallType.OnClick)
                        {
                            if (infomations[i].target != null)
                            {
                                if (infomations[i].target.GetComponent<Button>() == null)
                                {
                                    Debug.LogError("Production target is not button");
                                    infomations[i].target = null;
                                }
                            }
                        }
                    }
                }
            }

            //= 기존 좌표, 스케일을 들고있게 하여 연산에 문제가 없게함.
            if (Selection.activeGameObject == this.gameObject)
            {
                if (infomations != null)
                {
                    for (int i = 0; i < infomations.Count; i++)
                    {
                        if (infomations[i].target != null)
                        {
                            infomations[i].initialPosition = infomations[i].target.anchoredPosition;
                            infomations[i].initialScale = infomations[i].target.localScale;

                            if (infomations[i].autoSetDuration)
                            {
                                infomations[i].duration = AutoDuration(infomations[i].animationType);
                            }
                        }
                    }
                }
            }
#endif
        }

        #region External functions
        public void Init(float width, float height)
        {
            //= Button Flag set
            for (int i = 0; i < infomations.Count; i++)
            {
                int index = i;
                Information info = infomations[index];

                if (info.callType == CallType.OnClick)
                {
                    Button button = info.target.GetComponent<Button>();

                    if (button != null)
                    {
                        button.onClick.AddListener(() =>
                        {
                            CallProduction(info);
                        });
                    }
                }
            }

            this.width = width;
            this.height = height;
        }
        public void CallOpenProcessing()
        {
            if (openningProduction == null)
            {
                openningProduction = StartCoroutine(OpenningProduction());
            }
        }

        public void CallCloseProduction()
        {
            if (closeProduction == null)
            {
                closeProduction = StartCoroutine(CloseProduction());
            }
        }

        public Tweener CallProduction(Information infomation)
        {
            RectTransform transform = infomation.target;

            if (infomation.hasTween != null)
            {
                infomation.hasTween.Kill(true);
            }

            switch (infomation.animationType)
            {
                case AnimationType.Punch:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOPunchScale(Vector3.one * 0.2f,
                        infomation.duration)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.Rotation:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.transform.DORotate(new Vector3(0, 0, -360) * (int)(infomation.duration + 1),
                        infomation.duration,
                        RotateMode.FastBeyond360)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                            infomation.target.rotation = Quaternion.Euler(
                                infomation.target.rotation.eulerAngles.x,
                                infomation.target.rotation.eulerAngles.y,
                                infomation.target.rotation.eulerAngles.z + (new Vector3(0, 0, 360) * (int)(infomation.duration + 1)).z);
                        });

                case AnimationType.ScrollDrop:
                    TransformSet(infomation,
                        infomation.initialPosition + new Vector3(0, height * 2),
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                        infomation.initialPosition,
                        infomation.duration)
                        .SetEase(Ease.InOutBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.ScrollUp:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                        infomation.initialPosition + new Vector3(0, height * 2),
                        infomation.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.ScrollDown:

                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                        infomation.initialPosition - new Vector3(0, height * 2),
                        infomation.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.ComeOut:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        Vector3.zero);

                    return infomation.hasTween = infomation.target.DOScale(
                        infomation.initialScale,
                        infomation.duration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.ComeIn:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOScale(
                        Vector3.zero,
                        infomation.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.EnterFromLeft:
                    TransformSet(infomation,
                        infomation.initialPosition - new Vector3(width, 0, 0),
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                        infomation.initialPosition,
                        infomation.duration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.EnterFromRight:
                    TransformSet(infomation,
                        infomation.initialPosition + new Vector3(width, 0, 0),
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                        infomation.initialPosition,
                        infomation.duration)
                        .SetEase(Ease.OutBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.OutToLeft:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOLocalMove(
                        infomation.initialPosition - new Vector3(width, 0, 0),
                        infomation.duration)
                        .SetEase(Ease.InBack)
                        .OnComplete(() =>
                        {
                            infomation.hasTween = null;
                        });

                case AnimationType.OutToRight:
                    TransformSet(infomation,
                        infomation.initialPosition,
                        infomation.initialScale);

                    return infomation.hasTween = infomation.target.DOAnchorPos(
                         infomation.initialPosition + new Vector3(width, 0, 0),
                         infomation.duration)
                         .SetEase(Ease.InBack)
                         .OnComplete(() =>
                         {
                             infomation.hasTween = null;
                         });
            }

            return null;

            void TransformSet(Information production, Vector3 position, Vector3 scale)
            {
                if (production.fixInitialTransform == false || production.target.localScale == Vector3.zero)
                {
                    production.target.transform.localScale = scale;
                }
                production.target.anchoredPosition = position;
            }
        }

        public void ForceCloseOn()
        {
            forceCloseFlag = true;
        }
        public void ForceCloseOff()
        {
            forceCloseFlag = false;
        }

        #endregion End - External functions

        private IEnumerator CloseProduction()
        {
            isCloseRunning = true;

            List<Information> infos = infomations.FindAll((v) => v.callType == CallType.OnClose);
            List<Tweener> closeTweeners = ListPool<Tweener>.New();

            if (infos != null && infos.Count != 0)
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    Tweener tween = CallProduction(infos[i]);
                    if (tween != null)
                    {
                        closeTweeners.Add(tween);
                    }
                }
            }
            else
            {
                //= External yield return : need it.
                yield return null;
                ListPool<Tweener>.Free(closeTweeners);
                closeProduction = null;
                isCloseRunning = false;
                yield break;
            }

            yield return new WaitUntil(() =>
            {
                if (forceCloseFlag) return true;
                if (closeTweeners == null) return true;

                return closeTweeners.TrueForAll((tween) =>
                {
                    return tween.IsPlaying() == false;
                });
            });

            if (forceCloseFlag)
            {
                if (closeTweeners != null)
                {
                    for (int i = 0; i < closeTweeners.Count; i++)
                    {
                        if (closeTweeners[i].IsPlaying())
                        {
                            closeTweeners[i].Kill();
                        }
                    }
                }

            }
            
            ListPool<Tweener>.Free(closeTweeners);

            //= Kill Coroutine flag
            closeProduction = null;
            isCloseRunning = false;
        }
        private IEnumerator OpenningProduction()
        {
            isOpenRunning = true;
            if (enabledControls == null || enabledControls.Count == 0) yield break;

            for (int i = 0; i < enabledControls.Count; i++)
            {
                enabledControls[i].enabled = false;
            }

            List<Information> infos = infomations.FindAll((v) => v.callType == CallType.OnOpen);
            List<Tweener> openningTweeners = ListPool<Tweener>.New();

            if (infos != null && infos.Count != 0)
            {
                for (int i = 0; i < infos.Count; i++)
                {
                    Tweener tween = CallProduction(infos[i]);
                    if (tween != null)
                    {
                        openningTweeners.Add(tween);
                    }
                }
            }
            else
            {
                yield return null;
                ListPool<Tweener>.Free(openningTweeners);
                openningProduction = null;
                isOpenRunning = false;
                yield break;
            }

            yield return new WaitUntil(() =>
            {
                if (openningTweeners == null) return true;

                return openningTweeners.TrueForAll((tween) =>
                {
                    return tween.IsPlaying() == false;
                });
            });

            for (int i = 0; i < enabledControls.Count; i++)
            {
                enabledControls[i].enabled = true;
            }

            ListPool<Tweener>.Free(openningTweeners);

            //== Kill Coroutine flag
            openningProduction = null;
            isOpenRunning = false;
        }

        private float AutoDuration(AnimationType types)
        {
            switch (types)
            {
                case AnimationType.Punch:
                case AnimationType.Rotation: return 0.5f;

                case AnimationType.ScrollDrop:
                case AnimationType.ScrollDown:
                case AnimationType.ScrollUp: return 1.0f;
                case AnimationType.ComeOut:
                case AnimationType.ComeIn:
                case AnimationType.EnterFromLeft:
                case AnimationType.EnterFromRight:
                case AnimationType.OutToLeft:
                case AnimationType.OutToRight: return 0.8f;
            }

            return 0.2f;
        }

    }

}
