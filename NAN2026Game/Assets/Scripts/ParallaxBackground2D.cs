using UnityEngine;

/// <summary>
/// 배경 타일 1장에 붙입니다.
/// 가로: 카메라 이동량의 일부만 따라가 시차(패럴랙스)를 만들고, 화면 밖으로 나가면 반대편으로 되돌려 무한 반복합니다.
/// 세로: 카메라를 거의 따라가게 해서 위아래로 움직여도 빈 공간이 생기지 않습니다.
/// autoScrollX 를 주면 카메라가 멈춰 있어도 계속 흐릅니다 (구름용).
/// 한 레이어는 타일 3장을 가로로 나란히 두는 것을 전제로 합니다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxBackground2D : MonoBehaviour
{
    [Header("시차 (0 = 카메라에 고정, 1 = 월드에 고정)")]
    [Range(0f, 1f)] public float parallaxX = 0.5f;

    [Tooltip("세로는 1에 가까울수록 카메라를 그대로 따라가 빈 공간이 안 생깁니다.")]
    [Range(0f, 1f)] public float parallaxY = 0.9f;

    [Header("자동 흐름 (구름 등)")]
    [Tooltip("초당 이동 유닛. 0이면 사용 안 함.")]
    public float autoScrollX = 0f;

    private Transform _cam;
    private float _startX;
    private float _baseY;
    private float _tileWidth;
    private float _drift;

    private void Start()
    {
        if (Camera.main != null) _cam = Camera.main.transform;
        _startX = transform.position.x;
        _baseY = transform.position.y;
        _tileWidth = GetComponent<SpriteRenderer>().bounds.size.x;
        if (_tileWidth <= 0.001f) _tileWidth = 1f;
    }

    private void LateUpdate()
    {
        if (_cam == null)
        {
            if (Camera.main == null) return;
            _cam = Camera.main.transform;
        }

        _drift += autoScrollX * Time.deltaTime;

        float x = _startX + _cam.position.x * parallaxX + _drift;
        float y = _baseY + _cam.position.y * parallaxY;
        transform.position = new Vector3(x, y, transform.position.z);

        // 타일 3장 기준으로 카메라 기준 ±1.5장을 벗어나면 반대편으로 되감기
        float dx = transform.position.x - _cam.position.x;
        if (dx > _tileWidth * 1.5f) _startX -= _tileWidth * 3f;
        else if (dx < -_tileWidth * 1.5f) _startX += _tileWidth * 3f;
    }
}
