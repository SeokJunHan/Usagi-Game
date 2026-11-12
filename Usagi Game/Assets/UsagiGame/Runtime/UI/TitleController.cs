using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UsagiGame
{
    public sealed class TitleController : MonoBehaviour
    {
        [SerializeField, Tooltip("최고 점수 텍스트입니다.")] private TMP_Text highScoreText;
        [SerializeField, Tooltip("설정 패널입니다.")] private GameObject settingsPanel;
        [SerializeField, Tooltip("BGM 토글입니다.")] private Toggle bgmToggle;
        [SerializeField, Tooltip("효과음 토글입니다.")] private Toggle sfxToggle;
        [SerializeField, Tooltip("진동 토글입니다.")] private Toggle vibrationToggle;
        [SerializeField, Tooltip("새 게임/이어하기 선택 패널입니다.")] private GameObject startChoicePanel;

        private void Start()
        {
            highScoreText.text = $"BEST  {LocalSave.HighScore:N0}";
            bgmToggle.SetIsOnWithoutNotify(LocalSave.BgmEnabled);
            sfxToggle.SetIsOnWithoutNotify(LocalSave.SfxEnabled);
            vibrationToggle.SetIsOnWithoutNotify(LocalSave.VibrationEnabled);
            settingsPanel.SetActive(false);
            if (startChoicePanel != null) startChoicePanel.SetActive(false);
        }

        public void StartGame()
        {
            if (LocalSave.HasCurrentGame)
            {
                if (startChoicePanel != null) startChoicePanel.SetActive(true);
                else Debug.LogError("TitleScene에 StartChoicePanel 연결이 필요합니다.", this);
                return;
            }

            StartNewGame();
        }

        public void StartNewGame()
        {
            LocalSave.DeleteCurrentGame();
            AudioListener.pause = false;
            SceneManager.LoadScene("GameScene");
        }

        public void ContinueGame()
        {
            if (!LocalSave.HasCurrentGame)
            {
                StartNewGame();
                return;
            }

            LocalSave.RequestResume();
            AudioListener.pause = false;
            SceneManager.LoadScene("GameScene");
        }

        public void CloseStartChoice() => startChoicePanel.SetActive(false);

        public void OpenSettings() => settingsPanel.SetActive(true);
        public void ShowSettings(bool show) => settingsPanel.SetActive(show);

        public void SaveSettings()
        {
            LocalSave.BgmEnabled = bgmToggle.isOn;
            LocalSave.SfxEnabled = sfxToggle.isOn;
            LocalSave.VibrationEnabled = vibrationToggle.isOn;
            LocalSave.SaveSettings();
            settingsPanel.SetActive(false);
        }
    }
}
