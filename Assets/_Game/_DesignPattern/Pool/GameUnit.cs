using UnityEngine;

namespace DesignPattern
{
    public abstract class GameUnit : MonoBehaviour
    {
        [SerializeField]
        private Transform tf;
        [SerializeField]
        private Transform skinTf;
        [SerializeField]
        private PoolType poolType;
        public PoolType PoolType => poolType;
        public Transform Tf => tf;
        public Transform SkinTf => skinTf;
        public RectTransform RectTf => (RectTransform)tf;
        public virtual void OnDespawn() { }
    }
}
