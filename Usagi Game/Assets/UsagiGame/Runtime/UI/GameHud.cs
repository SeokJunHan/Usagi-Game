using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UsagiGame
{
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField, Tooltip("현재 점수 텍스트입니다.")] private TMP_Text scoreText;
        [SerializeField, Tooltip("최고 점수 텍스트입니다.")] private TMP_Text highScoreText;
        [SerializeField, Tooltip("NEXT 과일 이미지입니다.")] private Image nextImage;
        [SerializeField, Tooltip("일시정지 패널입니다.")] private GameObject pausePanel;
        [SerializeField, Tooltip("결과 패널입니다.")] private GameObject resultPanel;
        [SerializeField, Tooltip("최종 점수 텍스트입니다.")] private TMP_Text resultScoreText;
        [SerializeField, Tooltip("결과 최고 점수 텍스트입니다.")] private TMP_Text resultHighScoreText;
        [SerializeField, Tooltip("신기록 표시입니다.")] private GameObject newRecordLabel;

        public void SetScore(int score)
        {
            scoreText.text = $"SCORE\n{score:N0}";
            highScoreText.text = $"BEST {Mathf.Max(LocalSave.HighScore, score):N0}";
        }

        public void SetNext(FruitDefinition definition)
        {
            nextImage.sprite = definition != null ? definition.Sprite : null;
            nextImage.enabled = definition != null;
        }

        public void ShowPlaying()
        {
            pausePanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void ShowPause()
        {
            pausePanel.SetActive(true);
            resultPanel.SetActive(false);
        }

        public void ShowResult(int score, bool newRecord)
        {
            pausePanel.SetActive(false);
            resultPanel.SetActive(true);
            resultScoreText.text = $"SCORE  {score:N0}";
            resultHighScoreText.text = $"BEST  {LocalSave.HighScore:N0}";
            newRecordLabel.SetActive(newRecord);
        }
    }
}
