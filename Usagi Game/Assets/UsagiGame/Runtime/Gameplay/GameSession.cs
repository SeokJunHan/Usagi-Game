using UnityEngine;
using UnityEngine.SceneManagement;

namespace UsagiGame
{
    public sealed class GameSession : MonoBehaviour
    {
        [SerializeField, Tooltip("과일 생성기입니다.")] private FruitSpawner spawner;
        [SerializeField, Tooltip("합체 처리기입니다.")] private MergeResolver mergeResolver;
        [SerializeField, Tooltip("넘침 감지기입니다.")] private OverflowDetector overflowDetector;
        [SerializeField, Tooltip("드롭 입력 처리기입니다.")] private DropController dropController;
        [SerializeField, Tooltip("게임 HUD입니다.")] private GameHud hud;

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }

        private void OnEnable()
        {
            mergeResolver.ScoreAwarded += AddScore;
            overflowDetector.Overflowed += GameOver;
            spawner.QueueChanged += OnQueueChanged;
            Application.focusChanged += OnFocusChanged;
        }

        private void OnDisable()
        {
            mergeResolver.ScoreAwarded -= AddScore;
            overflowDetector.Overflowed -= GameOver;
            spawner.QueueChanged -= OnQueueChanged;
            Application.focusChanged -= OnFocusChanged;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (!spawner.Initialize(mergeResolver))
            {
                enabled = false;
                return;
            }
            if (LocalSave.ConsumeResumeRequest() && LocalSave.TryLoadCurrentGame(out GameSaveData data))
                RestoreGame(data);
            else
                StartGame();
        }

        public void StartGame()
        {
            LocalSave.DeleteCurrentGame();
            Time.timeScale = 1f;
            Score = 0;
            mergeResolver.ResetResolver();
            overflowDetector.ResetDetector();
            spawner.Clear();
            dropController.ResetController();
            State = GameState.Playing;
            dropController.SetRunning(true);
            overflowDetector.SetRunning(true);
            hud.SetScore(Score);
            hud.SetNext(spawner.Next);
            hud.ShowPlaying();
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            dropController.SetRunning(false);
            overflowDetector.SetRunning(false);
            Time.timeScale = 0f;
            hud.ShowPause();
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            State = GameState.Playing;
            dropController.SetRunning(true);
            overflowDetector.SetRunning(true);
            hud.ShowPlaying();
        }

        public void GameOver()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            dropController.SetRunning(false);
            overflowDetector.SetRunning(false);
            Time.timeScale = 0f;
            bool newRecord = LocalSave.TrySetHighScore(Score);
            LocalSave.DeleteCurrentGame();
            hud.ShowResult(Score, newRecord);
        }

        public void GoToTitle()
        {
            SaveCurrentGame();
            Time.timeScale = 1f;
            SceneManager.LoadScene("TitleScene");
        }

        private void AddScore(int value)
        {
            Score += value;
            hud.SetScore(Score);
        }

        private void OnQueueChanged(FruitDefinition current, FruitDefinition next) => hud.SetNext(next);
        private void RestoreGame(GameSaveData data)
        {
            Time.timeScale = 1f;
            Score = Mathf.Max(0, data.Score);
            mergeResolver.ResetResolver();
            overflowDetector.ResetDetector();
            dropController.ResetController();
            if (!spawner.Restore(data, mergeResolver.PhysicsStep))
            {
                StartGame();
                return;
            }
            State = GameState.Playing;
            dropController.SetRunning(true);
            overflowDetector.SetRunning(true);
            hud.SetScore(Score);
            hud.SetNext(spawner.Next);
            hud.ShowPlaying();
        }

        private void SaveCurrentGame()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;

            var fruits = new FruitSaveData[spawner.Active.Count];
            for (int i = 0; i < fruits.Length; i++)
            {
                FruitActor actor = spawner.Active[i];
                fruits[i] = new FruitSaveData
                {
                    Stage = actor.Definition.Stage,
                    Position = actor.transform.position,
                    Rotation = actor.transform.eulerAngles.z,
                    Velocity = actor.Body.linearVelocity,
                    AngularVelocity = actor.Body.angularVelocity
                };
            }

            LocalSave.SaveCurrentGame(new GameSaveData
            {
                Score = Score,
                CurrentStage = spawner.Current.Stage,
                NextStage = spawner.Next.Stage,
                Fruits = fruits
            });
        }

        private void OnFocusChanged(bool focused)
        {
            if (focused) return;
            SaveCurrentGame();
            Pause();
        }
    }
}
