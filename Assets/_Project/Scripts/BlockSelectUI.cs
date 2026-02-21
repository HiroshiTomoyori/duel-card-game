using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockSelectUI : MonoBehaviour
{
    public static BlockSelectUI I;

    [Header("UI")]
    [SerializeField] GameObject panel;
    [SerializeField] Transform candidatesRoot;
    [SerializeField] BlockerChoiceButton buttonPrefab;

    CardController selected;
    bool decided;

    void Awake()
    {
        I = this;
        if (panel != null) panel.SetActive(false);
    }

    public IEnumerator ChooseBlocker(List<CardController> candidates, Action<CardController> onDecide)
    {
        selected = null;
        decided = false;

        Show(candidates);

        while (!decided) yield return null;

        Hide();
        onDecide?.Invoke(selected);
    }

    void Show(List<CardController> candidates)
    {
        Debug.Log($"[BlockUI] Show candidates={candidates?.Count ?? -1} rootChildren={candidatesRoot.childCount} prefab={(buttonPrefab ? buttonPrefab.name : "null")}");

        if (panel != null) panel.SetActive(true);

        if (candidatesRoot != null)
        {
            for (int i = candidatesRoot.childCount - 1; i >= 0; i--)
                Destroy(candidatesRoot.GetChild(i).gameObject);
        }

        if (buttonPrefab != null && candidatesRoot != null)
        {
            foreach (var c in candidates)
            {
                Debug.Log($"[BlockUI] Spawn button for {c.name}");
                var btn = Instantiate(buttonPrefab, candidatesRoot);
                btn.Setup(c, OnPick);
            }
        }
    }

    void Hide()
    {
        if (panel != null) panel.SetActive(false);

        if (candidatesRoot != null)
        {
            for (int i = candidatesRoot.childCount - 1; i >= 0; i--)
                Destroy(candidatesRoot.GetChild(i).gameObject);
        }
    }

    void OnPick(CardController c)
    {
        selected = c;
        decided = true;
    }

    public void OnClickNoBlock()
    {
        selected = null;
        decided = true;
    }
}
