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
        public void Register(int hash);
        public void UnRegister(int hash);
        public void CallObservers(System.Action<UIObserver> action);
        public void ShowOrder(List<int> hashs);
    }

    public interface UIObserver : StateControl
    {
        public void Init(float w, float h, int hash, Subject subject);
    }
}
