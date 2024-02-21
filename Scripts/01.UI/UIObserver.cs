using System.Collections.Generic;

namespace UI.Inherited.Interface
{
    public interface StateControl
    {
        public void ShowState();
        public string GetState();
    }

    public interface Subject : StateControl
    {
        public void Register(UIObserver observer);
        public void UnRegister(UIObserver observer);
        public void CallObservers(System.Action<UIObserver> action);
    }

    public interface UIObserver : StateControl
    {
        public void Init(float w, float h, int hash, Subject subject);
    }
}
