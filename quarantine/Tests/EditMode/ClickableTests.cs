using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableTests
{
    [Test]
    public void OnPointerClick_InvokesOnClick()
    {
        var go = new GameObject("clickable");
        var clickable = go.AddComponent<Clickable>();
        int calls = 0;
        clickable.onClick.AddListener(() => calls++);

        clickable.OnPointerClick(new PointerEventData(EventSystem.current));

        Assert.AreEqual(1, calls);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void OnPointerClick_WhenNotInteractable_DoesNotInvoke()
    {
        var go = new GameObject("clickable");
        var clickable = go.AddComponent<Clickable>();
        clickable.Interactable = false;
        int calls = 0;
        clickable.onClick.AddListener(() => calls++);

        clickable.OnPointerClick(new PointerEventData(EventSystem.current));

        Assert.AreEqual(0, calls);
        Object.DestroyImmediate(go);
    }
}
