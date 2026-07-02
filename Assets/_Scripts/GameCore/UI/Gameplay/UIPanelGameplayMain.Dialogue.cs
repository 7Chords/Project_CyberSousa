using System.Collections.Generic;
using DG.Tweening;
using GameCore.RefData;
using SCFrame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCore.UI
{
    public partial class UIPanelGameplayMain
    {

        private void StartDialogue(long dialogueStartId)
        {
            ClearDialogueOptions();
            _isDialogueRunning = true;
            SetCurrentDialogue(dialogueStartId);
        }


        private void SetCurrentDialogue(long dialogueId)
        {
            ClearPendingDialogueAdvanceAfterPlayerLine();
            if (!_dialogueMap.TryGetValue(dialogueId, out DialogueRefData dialogueRefData))
            {
                Debug.LogError($"Gameplay 获取对话配置失败：未找到 dialogueId={dialogueId}");
                EndDialogue();
                return;
            }

            _currentDialogueRefData = dialogueRefData;
            if (dialogueRefData.dialogueType != EDialogueType.SELECT)
                ApplyDialogueFavorEffects(dialogueRefData);

            if (dialogueRefData.dialogueType == EDialogueType.SELECT)
            {
                ClearDialogueOptions();
                ShowDialogueSelectionFromNode(dialogueRefData);
                return;
            }

            ClearDialogueOptions();
            if (ShouldPrepareDialogueOptions(dialogueRefData))
                PrepareDialogueOptions(dialogueRefData);

            RefreshAllUi();
        }


        private bool ShouldPrepareDialogueOptions(DialogueRefData dialogueRefData)
        {
            if (dialogueRefData?.nextList == null || dialogueRefData.nextList.Count == 0)
                return false;

            for (int index = 0; index < dialogueRefData.nextList.Count; index++)
            {
                long nextDialogueId = dialogueRefData.nextList[index];
                if (nextDialogueId == 0)
                    continue;

                DialogueRefData nextDialogueRefData = GetDialogueRefData(nextDialogueId);
                if (nextDialogueRefData != null && nextDialogueRefData.dialogueType == EDialogueType.SELECT)
                    return true;
            }

            return false;
        }


        private void PrepareDialogueOptions(DialogueRefData dialogueRefData)
        {
            ClearDialogueOptions();
            if (dialogueRefData == null || dialogueRefData.nextList == null || dialogueRefData.nextList.Count == 0)
                return;

            for (int index = 0; index < dialogueRefData.nextList.Count; index++)
            {
                long nextDialogueId = dialogueRefData.nextList[index];
                if (nextDialogueId == 0)
                    continue;

                DialogueRefData optionDialogueRefData = GetDialogueRefData(nextDialogueId);
                if (optionDialogueRefData == null)
                    continue;

                if (optionDialogueRefData.dialogueType != EDialogueType.SELECT)
                {
                    Debug.LogError($"Gameplay 对话分支结构异常：dialogueId={dialogueRefData.id} 的 nextList[{index}]={nextDialogueId} 不是 SELECT 节点。");
                    continue;
                }

                _currentOptionDialogueList.Add(optionDialogueRefData);
            }

            if (_currentOptionDialogueList.Count == 0)
                Debug.LogError($"Gameplay 对话分支初始化失败：dialogueId={dialogueRefData.id} 没有可用的选项节点。");
        }


        private void ClearDialogueOptions()
        {
            _currentOptionDialogueList.Clear();
            HideAllDialogueOptionItems();
        }


        private DialogueRefData GetDialogueRefData(long dialogueId)
        {
            if (_dialogueMap.TryGetValue(dialogueId, out DialogueRefData dialogueRefData))
                return dialogueRefData;

            Debug.LogError($"Gameplay 获取对话节点失败：未找到 dialogueId={dialogueId}");
            return null;
        }


        private void ShowDialogueSelectionFromNode(DialogueRefData dialogueRefData)
        {
            _currentDialogueRefData = dialogueRefData;
            RefreshAllUi();
        }


        private void EndDialogue()
        {
            StopDialoguePresentation();
            _isDialogueRunning = false;
            _currentDialogueRefData = null;
            ClearDialogueOptions();

            GameTimeMgr.instance.AddTimeInstant(_currentNeedRefData?.timeEffect);

            // TODO: 这里可以接 CustomerNeedMgr.EvaluateDialogue，根据当前好感度触发额外需求对话。

            RefreshAllUi();
            TryStartFirstCustomerGuide();
        }


        private bool IsPlayerDialogue(DialogueRefData dialogueRefData)
        {
            return dialogueRefData != null && dialogueRefData.name == PlayerSpeakerName;
        }


        private bool CanAdvanceDialogueByClick()
        {
            if (_currentDialogueRefData == null)
                return false;

            if (_awaitDialogueAdvanceAfterPlayerLine)
                return !_isDialogueTypewriterPlaying;

            if (_isDialogueTypewriterPlaying)
                return false;

            if (_currentOptionDialogueList.Count > 0)
                return false;

            if (_currentDialogueRefData.flagType == EDialogueFlagType.END)
                return true;

            return _currentDialogueRefData.nextList != null && _currentDialogueRefData.nextList.Count == 1;
        }


        private static int ParseDialogueOptionIndex(object[] args)
        {
            if (args == null || args.Length == 0 || args[0] == null)
                return -1;

            if (args[0] is int optionIndex)
                return optionIndex;

            if (int.TryParse(args[0].ToString(), out optionIndex))
                return optionIndex;

            return -1;
        }


        private void HandleDialogueOptionSelection(DialogueRefData optionDialogueRefData)
        {
            if (optionDialogueRefData == null)
            {
                Debug.LogError("Gameplay 选择对话选项失败：选项节点为空。");
                return;
            }

            GamePlayerDataMgr.instance.RecordDialogueOptionSelection(optionDialogueRefData.id);
            ApplyDialogueFavorEffects(optionDialogueRefData);
            ClearDialogueOptions();
            _awaitDialogueAdvanceAfterPlayerLine = true;
            if (optionDialogueRefData.nextList == null || optionDialogueRefData.nextList.Count == 0 || optionDialogueRefData.nextList[0] == 0)
            {
                _pendingEndDialogueAfterPlayerLine = true;
                _pendingDialogueIdAfterPlayerLine = 0;
            }
            else
            {
                _pendingEndDialogueAfterPlayerLine = false;
                _pendingDialogueIdAfterPlayerLine = optionDialogueRefData.nextList[0];
            }

            PlayPlayerDialogueEnterAnimation(optionDialogueRefData.id);
            StartDialogueTypewriter(mono.txtDialogueRight, optionDialogueRefData.content, optionDialogueRefData.id);
            if (mono.txtDialogueLeft != null)
                mono.txtDialogueLeft.text = string.Empty;
            UpdateDialogueStripVisibility();
            RefreshDialogueAdvanceClickArea();
        }

        private void ApplyDialogueFavorEffects(DialogueRefData dialogueRefData)
        {
            if (dialogueRefData == null)
                return;

            _currentAffectValue += dialogueRefData.affectValue;
            AddCurrentCustomerFavor(dialogueRefData.affectValue);

            List<NpcFavorEffectData> extraFavorEffectList = dialogueRefData.extraFavorEffectList;
            if (extraFavorEffectList == null || extraFavorEffectList.Count == 0)
                return;

            for (int index = 0; index < extraFavorEffectList.Count; index++)
            {
                NpcFavorEffectData extraFavorEffectData = extraFavorEffectList[index];
                if (extraFavorEffectData == null)
                {
                    Debug.LogError($"Gameplay 对话额外好感配置为空：dialogueId={dialogueRefData.id}，index={index}");
                    continue;
                }

                if (extraFavorEffectData.npcId <= 0)
                {
                    Debug.LogError($"Gameplay 对话额外好感配置无效：dialogueId={dialogueRefData.id}，npcId={extraFavorEffectData.npcId}");
                    continue;
                }

                if (extraFavorEffectData.favorValue == 0)
                    continue;

                GamePlayerDataMgr.instance.AddNpcFavor(extraFavorEffectData.npcId, extraFavorEffectData.favorValue);
            }
        }


        private void EnsureDialogueOptionPrefab()
        {
            if (_dialogueOptionPrefabReady && _dialogueOptionItemPrefab != null)
                return;

            if (string.IsNullOrEmpty(mono.dialogueOptionItemResName))
            {
                Debug.LogError("Gameplay 对话选项预制体未配置：请在 UIMonoGameplayMain 上填写 Addressables 资源名。");
                return;
            }

            _dialogueOptionItemPrefab = ResourcesHelper.LoadAsset<GameObject>(mono.dialogueOptionItemResName);
            if (_dialogueOptionItemPrefab == null)
            {
                Debug.LogError($"Gameplay 加载对话选项预制体失败：resName={mono.dialogueOptionItemResName}");
                return;
            }

            if (_dialogueOptionItemPrefab.GetComponent<UIMonoDialogueOption>() == null)
            {
                Debug.LogError($"Gameplay 对话选项预制体缺少 UIMonoDialogueOption 组件：resName={mono.dialogueOptionItemResName}");
                _dialogueOptionItemPrefab = null;
                return;
            }

            _dialogueOptionPrefabReady = true;
            WarmupDialogueOptionPool();
        }


        private void WarmupDialogueOptionPool(int count = 2)
        {
            if (_dialogueOptionItemPrefab == null || mono.dialogueOptionRoot == null)
                return;

            if (_dialogueOptionItemPool.Count >= count)
                return;

            bool rootWasActive = mono.dialogueOptionRoot.gameObject.activeSelf;
            mono.dialogueOptionRoot.gameObject.SetActive(true);
            while (_dialogueOptionItemPool.Count < count)
                GetOrCreateDialogueOptionItem(_dialogueOptionItemPool.Count);

            HideAllDialogueOptionItems();
            mono.dialogueOptionRoot.gameObject.SetActive(rootWasActive);
        }


        private UIMonoDialogueOption GetOrCreateDialogueOptionItem(int index)
        {
            while (_dialogueOptionItemPool.Count <= index)
            {
                if (_dialogueOptionItemPrefab == null || mono.dialogueOptionRoot == null)
                    return null;

                GameObject instanceObject = Object.Instantiate(_dialogueOptionItemPrefab, mono.dialogueOptionRoot);
                instanceObject.name = $"DialogueOption_{index}";
                UIMonoDialogueOption optionItem = instanceObject.GetComponent<UIMonoDialogueOption>();
                if (optionItem == null)
                {
                    Debug.LogError("Gameplay 实例化对话选项失败：缺少 UIMonoDialogueOption 组件。");
                    Object.Destroy(instanceObject);
                    return null;
                }

                optionItem.EnsureReferences();
                _dialogueOptionItemPool.Add(optionItem);
            }

            return _dialogueOptionItemPool[index];
        }


        private void RebuildDialogueOptionLayoutsImmediate()
        {
            if (mono.dialogueOptionRoot == null)
                return;

            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem != null && optionItem.gameObject.activeSelf)
                    optionItem.RefreshLayout();
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(mono.dialogueOptionRoot);
        }


        private void BindDialogueOptionItem(UIMonoDialogueOption optionItem, int optionIndex)
        {
            if (optionItem == null || optionItem.btnOption == null)
            {
                Debug.LogError("Gameplay 绑定对话选项失败：选项按钮为空。");
                return;
            }

            optionItem.btnOption.RemoveAllListener(ESCEventType.ON_MOUSE_LEFT_CLICK_DOWN);
            optionItem.btnOption.AddMouseLeftClickDown(OnDialogueOptionClicked, optionIndex);
        }


        private void UnbindAllDialogueOptionItems()
        {
            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                optionItem?.btnOption?.RemoveAllListener(ESCEventType.ON_MOUSE_LEFT_CLICK_DOWN);
            }
        }

        private void HideAllDialogueOptionItems()
        {
            UnbindAllDialogueOptionItems();
            for (int index = 0; index < _dialogueOptionItemPool.Count; index++)
            {
                UIMonoDialogueOption optionItem = _dialogueOptionItemPool[index];
                if (optionItem != null)
                    optionItem.gameObject.SetActive(false);
            }
        }


        private void RefreshCustomerPortrait()
        {
            ApplyCustomerPortrait(mono.imgDialoguePortrait, ResolveCustomerPortraitResName(_useCustomerBackPortrait));
        }


        private void SwitchCustomerPortraitToBack()
        {
            if (_currentCustomerRefData == null)
                return;

            _useCustomerBackPortrait = true;
            RefreshCustomerPortrait();
        }


        private string ResolveCustomerPortraitResName(bool useBack)
        {
            if (_currentCustomerRefData == null)
                return null;

            if (useBack && IsValidPortraitResName(_currentCustomerRefData.portraitBackResName))
                return _currentCustomerRefData.portraitBackResName;

            if (IsValidPortraitResName(_currentCustomerRefData.portraitFrontResName))
                return _currentCustomerRefData.portraitFrontResName;

            return null;
        }


        private void ClearDialoguePortraits()
        {
            _useCustomerBackPortrait = false;
            _activePortraitResName = null;
            ResetCustomerPortraitTransform();
            ApplyCustomerPortrait(mono.imgDialoguePortrait, null);
        }


        private void ApplyCustomerPortrait(Image image, string portraitResName)
        {
            if (image == null)
                return;

            if (!IsValidPortraitResName(portraitResName))
            {
                image.enabled = false;
                image.sprite = null;
                return;
            }

            Sprite sprite = GetOrLoadPortraitSprite(portraitResName);
            if (sprite == null)
            {
                image.enabled = false;
                image.sprite = null;
                return;
            }

            _activePortraitResName = portraitResName;
            image.sprite = sprite;
            image.preserveAspect = true;
            image.enabled = true;
        }


        private static bool IsValidPortraitResName(string portraitResName)
        {
            return !string.IsNullOrEmpty(portraitResName) && portraitResName != "*";
        }


        private Sprite GetOrLoadPortraitSprite(string portraitResName)
        {
            if (_portraitSpriteCache.TryGetValue(portraitResName, out Sprite cachedSprite))
                return cachedSprite;

            Sprite sprite = ResourcesHelper.LoadAsset<Sprite>(portraitResName);
            if (sprite == null)
            {
                Debug.LogError($"Gameplay 加载住户立绘失败：portraitResName={portraitResName}");
                return null;
            }

            _portraitSpriteCache[portraitResName] = sprite;
            return sprite;
        }
    }
}
