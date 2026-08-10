using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro; // TextMeshPro를 사용하는 경우

public class PauseMenuAudioAndResolution : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string MasterParam = "MasterVolume";
    [SerializeField] private string bgmParamName = "BGMVolume";
    [SerializeField] private string sfxParamName = "SFXVolume";

    [Space(5)]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Button bgmMinusBtn;
    [SerializeField] private Button bgmPlusBtn;

    [Space(5)]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Button sfxMinusBtn;
    [SerializeField] private Button sfxPlusBtn;

    [SerializeField] private float volumeStep = 0.1f; // 증감 버튼 누를 때 변경량 (10%)

    [Header("Resolution Settings")]
    [SerializeField] private TMP_Dropdown resolutionDropdown; // TextMeshPro Dropdown 사용 시
    // public Dropdown resolutionDropdown; // 일반 UI Dropdown 사용 시 위 줄 주석 처리 후 이 줄 주석 해제

    private readonly List<(int width, int height)> resolutions = new List<(int, int)>
    {
        (1280, 720),   // HD (16:9)
        (1600, 900),   // HD+ (16:9)
        (1920, 1080),  // FHD (16:9 - 가장 대중적)
        (2560, 1440),  // QHD (16:9)
        (3840, 2160)   // 4K (16:9)
    };

    private void Start()
    {
        InitAudioUI();
        InitResolutionUI();
    }

    #region Audio Logic

    private void InitAudioUI()
    {
        // 슬라이더 기본값 설정 (0.0001 ~ 1 범위)
        bgmSlider.minValue = 0.0001f;
        bgmSlider.maxValue = 1f;
        sfxSlider.minValue = 0.0001f;
        sfxSlider.maxValue = 1f;

        // 저장된 음량 불러오기 (기본값 0.75f)
        float savedBGM = PlayerPrefs.GetFloat("BGMVolume", 0.75f);
        float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 0.75f);

        bgmSlider.value = savedBGM;
        sfxSlider.value = savedSFX;

        SetBGMVolume(savedBGM);
        SetSFXVolume(savedSFX);

        // 이벤트 리스너 연결
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        if (bgmMinusBtn != null) bgmMinusBtn.onClick.AddListener(() => ChangeSliderValue(bgmSlider, -volumeStep));
        if (bgmPlusBtn != null) bgmPlusBtn.onClick.AddListener(() => ChangeSliderValue(bgmSlider, volumeStep));
        if (sfxMinusBtn != null) sfxMinusBtn.onClick.AddListener(() => ChangeSliderValue(sfxSlider, -volumeStep));
        if (sfxPlusBtn != null) sfxPlusBtn.onClick.AddListener(() => ChangeSliderValue(sfxSlider, volumeStep));
    }

    public void SetBGMVolume(float value)
    {
        // Linear 값(0.0001~1)을 Decibel(-80~0)로 변환
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        if (audioMixer != null) audioMixer.SetFloat(bgmParamName, dB);
        PlayerPrefs.SetFloat("BGMVolume", value);
    }

    public void SetSFXVolume(float value)
    {
        float dB = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        if (audioMixer != null) audioMixer.SetFloat(sfxParamName, dB);
        PlayerPrefs.SetFloat("SFXVolume", value);
    }

    private void ChangeSliderValue(Slider slider, float delta)
    {
        slider.value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
    }

    #endregion

    #region Resolution Logic

    private void InitResolutionUI()
    {
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 2; // 기본값 1920x1080

        int savedWidth = PlayerPrefs.GetInt("ResWidth", Screen.width);
        int savedHeight = PlayerPrefs.GetInt("ResHeight", Screen.height);

        for (int i = 0; i < resolutions.Count; i++)
        {
            string option = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(option);

            if (resolutions[i].width == savedWidth && resolutions[i].height == savedHeight)
            {
                currentResIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();

        // 저장된 해상도로 초기화 적용
        SetResolution(currentResIndex);

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= resolutions.Count) return;

        var targetRes = resolutions[index];
        Screen.SetResolution(targetRes.width, targetRes.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt("ResWidth", targetRes.width);
        PlayerPrefs.SetInt("ResHeight", targetRes.height);
    }

    #endregion
}