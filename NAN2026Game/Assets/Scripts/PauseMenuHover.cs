using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 마우스가 버튼 위에 올라오면 PauseMenu 의 선택 항목을 이 버튼으로 바꿉니다.
/// 키보드 선택과 마우스 선택이 항상 같은 항목을 가리키게 하기 위한 것입니다.
/// </summary>
public class PauseMenuHover : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private PauseMenu menu;
    [SerializeField] private int index;

    public void Setup(PauseMenu owner, int i)
    {
        menu = owner;
        index = i;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menu != null) menu.SetIndex(index);
    }
}
