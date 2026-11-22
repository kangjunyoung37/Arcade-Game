using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MagazineItemView : MonoBehaviour
{
    private const string RoundCountFormat = "{0}/{1}";

    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text roundCountText;

    private MagazineItemInstance _item;
    private bool _subscribed;

    public MagazineItemInstance Item => _item;

    /// <summary>
    /// 표시할 실제 탄창을 연결합니다.
    /// 이 함수에서는 탄창을 생성하지 않습니다.
    /// </summary>
    public void Bind(MagazineItemInstance item)
    {
        Unsubscribe();

        _item = item;

        if (isActiveAndEnabled)
            Subscribe();

        Refresh();
    }

    private void OnEnable()
    {
        Subscribe();
        Refresh();
    }

    private void OnDisable()
    {
        // UI를 닫아도 _item은 유지합니다.
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        _item = null;
    }

    private void Subscribe()
    {
        if (_subscribed || _item == null)
            return;

        _item.Changed += Refresh;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed)
            return;

        if (_item != null)
            _item.Changed -= Refresh;

        _subscribed = false;
    }

    /// <summary>
    /// 연결된 탄창의 현재 상태를 화면에 반영합니다.
    /// </summary>
    private void Refresh()
    {
        if (_item == null)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameText != null)
                nameText.SetText(string.Empty);

            if (roundCountText != null)
                roundCountText.SetText(string.Empty);

            return;
        }

        MagazineData data = _item.Data;

        if (iconImage != null)
        {
            Sprite icon = data != null ? data.Icon : null;

            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (nameText != null)
        {
            nameText.SetText(data != null ? data.DisplayName : string.Empty);
        }

        if (roundCountText != null)
        {
            roundCountText.SetText( RoundCountFormat, _item.RoundCount, _item.Capacity);
        }
    }
}