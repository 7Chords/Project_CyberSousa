using System.Collections;
using UnityEngine;

namespace SCFrame
{
    /// <summary>
    /// 协程条目：封装一次 Unity 协程的运行与回收。
    /// </summary>
    public class CoroutineItem
    {
        public object owner { get; private set; }
        public IEnumerator enumerator { get; private set; }
        public string coroutineId { get; private set; }
        public bool isRunning { get; private set; }

        private Coroutine _m_unityCoroutine;

        public CoroutineItem(object _owner, IEnumerator _enumerator, string _coroutineId)
        {
            this.owner = _owner;
            this.enumerator = _enumerator;
            this.coroutineId = _coroutineId;
            isRunning = false;
        }

        /// <summary>由 <see cref="SCTaskHelper"/> 启动包装协程。</summary>
        public void Start()
        {
            if (isRunning || SCTaskHelper.instance == null)
                return;

            isRunning = true;
            _m_unityCoroutine = SCTaskHelper.instance.StartCoroutine(RunWrapper());
        }

        /// <summary>
        /// 跑完用户迭代器后 KillCoroutine。
        /// </summary>
        private IEnumerator RunWrapper()
        {
            yield return enumerator;
            isRunning = false;
            SCTaskHelper.instance?.KillCoroutine(coroutineId);
        }

        /// <summary>停止底层 Unity 协程。</summary>
        public void Stop()
        {
            isRunning = false;
            if (_m_unityCoroutine != null && SCTaskHelper.instance != null)
            {
                SCTaskHelper.instance.StopCoroutine(_m_unityCoroutine);
            }
            enumerator = null;
        }
    }
}
