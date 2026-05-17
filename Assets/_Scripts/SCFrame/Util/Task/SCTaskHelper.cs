using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SCFrame
{
    /// <summary>
    /// SCFrame 任务助手：为非 Mono 提供 Update / LateUpdate / FixedUpdate、延迟到下一帧执行及协程管理。
    /// </summary>
    public class SCTaskHelper : SingletonPersistent<SCTaskHelper>
    {
        private Action _m_updateEvent;
        private Action _m_lateUpdateEvent;
        private Action _m_fixedUpdateEvent;

        /// <summary>延迟到下一帧再执行的委托队列。</summary>
        private Queue<Action> _m_nextUpdateActionQueue;
        private Queue<Action> _m_nextLateUpdateActionQueue;
        private Queue<Action> _m_nextFixedUpdateActionQueue;

        private TweenContainer _m_tweenContainer;

        // 协程：ID → 条目；拥有者 → ID 列表
        private Dictionary<string, CoroutineItem> _m_coroutineDict;
        private Dictionary<object, List<string>> _m_ownerCoroutineMap;
        private long _m_coroutineIdCounter;
        public override void OnInitialize()
        {
            _m_nextUpdateActionQueue = new Queue<Action>();
            _m_nextLateUpdateActionQueue = new Queue<Action>();
            _m_nextFixedUpdateActionQueue = new Queue<Action>();
            _m_tweenContainer = new TweenContainer();

            _m_coroutineDict = new Dictionary<string, CoroutineItem>();
            _m_ownerCoroutineMap = new Dictionary<object, List<string>>();
            _m_coroutineIdCounter = 0;
        }

        public override void OnDiscard()
        {
            _m_updateEvent = null;
            _m_lateUpdateEvent = null;
            _m_fixedUpdateEvent = null;
            _m_nextUpdateActionQueue.Clear();
            _m_nextLateUpdateActionQueue.Clear();
            _m_nextFixedUpdateActionQueue.Clear();
            _m_nextUpdateActionQueue = null;
            _m_nextLateUpdateActionQueue = null;
            _m_nextFixedUpdateActionQueue = null;

            _m_tweenContainer?.KillAllDoTween();
            _m_tweenContainer = null;

            ClearAllCoroutines();
            _m_coroutineDict = null;
            _m_ownerCoroutineMap = null;
        }


        // =============== 帧回调监听 ===============

        /// <summary>注册 Update 回调。</summary>
        public void AddUpdateListener(Action _action)
        {
            _m_updateEvent += _action;
        }
        /// <summary>移除 Update 回调。</summary>
        public void RemoveUpdateListener(Action _action)
        {
            _m_updateEvent -= _action;
        }

        /// <summary>注册 LateUpdate 回调。</summary>
        public void AddLateUpdateListener(Action _action)
        {
            _m_lateUpdateEvent += _action;
        }
        /// <summary>移除 LateUpdate 回调。</summary>
        public void RemoveLateUpdateListener(Action _action)
        {
            _m_lateUpdateEvent -= _action;
        }

        /// <summary>注册 FixedUpdate 回调。</summary>
        public void AddFixedUpdateListener(Action _action)
        {
            _m_fixedUpdateEvent += _action;
        }
        /// <summary>移除 FixedUpdate 回调。</summary>
        public void RemoveFixedUpdateListener(Action _action)
        {
            _m_fixedUpdateEvent -= _action;
        }

        public void ClearAllUpdateListener()
        {
            _m_updateEvent = null;
        }
        public void ClearAllFixedUpdateListener()
        {
            _m_fixedUpdateEvent = null;
        }
        public void ClearAllLateUpdateListener()
        {
            _m_lateUpdateEvent = null;
        }

        private void Update()
        {
            _m_updateEvent?.Invoke();
            if (_m_nextUpdateActionQueue == null) return;
            executeQueuedActions(_m_nextUpdateActionQueue);
        }
        private void LateUpdate()
        {
            _m_lateUpdateEvent?.Invoke();
            if (_m_nextLateUpdateActionQueue == null) return;
            executeQueuedActions(_m_nextLateUpdateActionQueue);

        }
        private void FixedUpdate()
        {
            _m_fixedUpdateEvent?.Invoke();
            if (_m_nextFixedUpdateActionQueue == null) return;
            executeQueuedActions(_m_nextFixedUpdateActionQueue);
        }

        /// <summary>在下一帧 Update 执行委托。</summary>
        public void DoInNextUpdate(Action _action)
        {
            if (_action != null)
            {
                _m_nextUpdateActionQueue.Enqueue(_action);
            }
        }

        /// <summary>在下一帧 FixedUpdate 执行委托。</summary>
        public void DoInNextFixedUpdate(Action _action)
        {
            if (_action != null)
            {
                _m_nextFixedUpdateActionQueue.Enqueue(_action);
            }
        }

        /// <summary>在下一帧 LateUpdate 执行委托。</summary>
        public void DoInNextLateUpdate(Action _action)
        {
            if (_action != null)
            {
                _m_nextLateUpdateActionQueue.Enqueue(_action);
            }
        }

        public void DoDelay(Action _action, float _delay)
        {
            if (_action == null)
                return;

            Tween tween = DOTween.Sequence().AppendInterval(_delay).OnComplete(() =>
            {
                _action.Invoke();
            });
            _m_tweenContainer?.RegDoTween(tween);

        }

        /// <summary>取出并执行队列中全部委托，然后清空队列。</summary>
        private void executeQueuedActions(Queue<Action> _actionsQueue)
        {
            if (_actionsQueue.Count > 0)
            {
                var actionsToExecute = new List<Action>(_actionsQueue);
                _actionsQueue.Clear();

                foreach (var action in actionsToExecute)
                {
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"SCTaskHelper:Error executing queued action: {ex.Message}");
                    }
                }
            }
        }

        // =============== 协程 ===============

        /// <summary>
        /// 启动协程。
        /// </summary>
        /// <param name="_owner">拥有者（便于按对象批量停止）</param>
        /// <param name="_enumerator">迭代器</param>
        /// <param name="_coroutineName">可选名称（会并入 ID）</param>
        /// <returns>协程 ID</returns>
        public string CreateCoroutine(object _owner, IEnumerator _enumerator, string _coroutineName = null)
        {
            if (_owner == null || _enumerator == null)
            {
                Debug.LogError("SCTaskHelper: Owner or enumerator is null!");
                return null;
            }

            string coroutineId = generateCoroutineId(_coroutineName);

            var coroutineItem = new CoroutineItem(_owner, _enumerator, coroutineId);
            _m_coroutineDict[coroutineId] = coroutineItem;

            if (!_m_ownerCoroutineMap.ContainsKey(_owner))
            {
                _m_ownerCoroutineMap[_owner] = new List<string>();
            }
            _m_ownerCoroutineMap[_owner].Add(coroutineId);

            coroutineItem.Start();

            return coroutineId;
        }

        /// <summary>按 ID 停止协程。</summary>
        public void KillCoroutine(string _coroutineId)
        {
            if (string.IsNullOrEmpty(_coroutineId) || !_m_coroutineDict.ContainsKey(_coroutineId))
                return;

            var coroutineItem = _m_coroutineDict[_coroutineId];
            coroutineItem.Stop();

            removeCoroutineInternal(_coroutineId, coroutineItem.owner);
        }

        /// <summary>停止某拥有者下的全部协程。</summary>
        public void KillAllCoroutines(object _owner)
        {
            if (_owner == null || !_m_ownerCoroutineMap.ContainsKey(_owner))
                return;

            var coroutineIds = new List<string>(_m_ownerCoroutineMap[_owner]);
            foreach (var coroutineId in coroutineIds)
            {
                if (_m_coroutineDict.ContainsKey(coroutineId))
                {
                    _m_coroutineDict[coroutineId].Stop();
                }
                removeCoroutineInternal(coroutineId, _owner);
            }
        }

        /// <summary>停止并清空所有协程。</summary>
        public void ClearAllCoroutines()
        {
            foreach (var coroutineItem in _m_coroutineDict.Values)
            {
                coroutineItem.Stop();
            }

            _m_coroutineDict.Clear();
            _m_ownerCoroutineMap.Clear();
        }

        /// <summary>协程是否仍在字典中（视为已创建）。</summary>
        public bool IsCoroutineRunning(string _coroutineId)
        {
            return !string.IsNullOrEmpty(_coroutineId) && _m_coroutineDict.ContainsKey(_coroutineId);
        }

        /// <summary>生成唯一协程 ID。</summary>
        private string generateCoroutineId(string _name = null)
        {
            _m_coroutineIdCounter++;
            string id = string.IsNullOrEmpty(_name) ?
                $"Coroutine_{_m_coroutineIdCounter}" :
                $"{_name}_{_m_coroutineIdCounter}";

            while (_m_coroutineDict.ContainsKey(id))
            {
                _m_coroutineIdCounter++;
                id = string.IsNullOrEmpty(_name) ?
                    $"Coroutine_{_m_coroutineIdCounter}" :
                    $"{_name}_{_m_coroutineIdCounter}";
            }

            return id;
        }

        /// <summary>内部：从字典与拥有者映射中移除记录。</summary>
        private void removeCoroutineInternal(string _coroutineId, object _owner)
        {
            _m_coroutineDict.Remove(_coroutineId);

            if (_m_ownerCoroutineMap.ContainsKey(_owner))
            {
                _m_ownerCoroutineMap[_owner].Remove(_coroutineId);
                if (_m_ownerCoroutineMap[_owner].Count == 0)
                {
                    _m_ownerCoroutineMap.Remove(_owner);
                }
            }
        }
    }
}
