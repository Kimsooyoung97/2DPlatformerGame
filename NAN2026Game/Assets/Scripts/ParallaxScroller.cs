using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RawImage의 uvRect를 이동시켜 배경 레이어를 무한 스크롤시킵니다.
/// 레이어별로 속도를 다르게 주면 패럴랙스(시차) 효과가 됩니다.
/// 텍스처의 Wrap Mode가 Repeat여야 합니다.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class ParallaxScroller : MonoBehaviour
{
    [Tooltip("초당 UV 이동량. 값이 클수록 빠르게 흐릅니다 (가까운 레이어일수록 크게).")]
    [SerializeField] private float speedX = 0.01f;
    [SerializeField] private float speedY = 0f;

    private RawImage _image;
    private Rect _baseUv;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
        _baseUv = _image.uvRect;
    }

    private void Update()
    {
        if (_image == null) return;

        Rect uv = _image.uvRect;
        uv.x += speedX * Time.unscaledDeltaTime;
        uv.y += speedY * Time.unscaledDeltaTime;

        // 좌표가 계속 커지면 float 정밀도가 떨어지므로 1.0 단위로 되감습니다.
        if (uv.x > 1f) uv.x -= 1f;
        else if (uv.x < -1f) uv.x += 1f;
        if (uv.y > 1f) uv.y -= 1f;
        else if (uv.y < -1f) uv.y += 1f;

        _image.uvRect = uv;
    }

    /// <summary>인스펙터에서 설정한 원래 UV 크기로 되돌립니다.</summary>
    public void ResetUv()
    {
        if (_image != null) _image.uvRect = _baseUv;
    }
}
