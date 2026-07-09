using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// シーン遷移をまたいだスコアの表示と、タイトル画面への帰還フローを制御する
public class ResultController : MonoBehaviour
{
    // 最終スコアを表示するためのテキストUIへの参照（Inspectorで割り当て）
    public Text ScoreText;

    private void Start()
    {
        // リザルト画面ではUI操作を行うため、ゲーム中のカーソルロックを解除する
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // タイトル画面と統一感を出すため、全てのテキストにフォント変更、太字、アウトラインを適用する
        Font uiFont = Resources.Load<Font>("Fonts/Consola") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text[] allTexts = FindObjectsOfType<Text>();
        foreach (Text t in allTexts)
        {
            t.font = uiFont;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            
            if (t.GetComponent<Outline>() == null)
            {
                Outline outline = t.gameObject.AddComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
                outline.effectDistance = new Vector2(4f, -4f); // 少し太めの縁取りにする
            }
        }

        // 非同期ロードやUI未割り当て時のNullReferenceバグを防ぐ防衛的処理
        if (ScoreText != null)
        {
            // 初期状態は0点にしておく
            ScoreText.text = "FINAL SCORE\n0";
            
            // コルーチンを使ってスコアのカウントアップと派手な演出を開始する
            StartCoroutine(AnimateScore(ScoreData.FinalScore));
        }

        // BGM/SE管理スクリプトが存在しない場合は動的生成（エディタからの直接起動時のエラー回避）
        if (FindObjectOfType<AudioManager>() == null)
        {
            GameObject audioObject = new GameObject("AudioManager");
            audioObject.AddComponent<AudioSource>();
            audioObject.AddComponent<AudioManager>();
        }
    }

    /// 入力: 最終スコア | 出力: IEnumerator | 副作用: スコアテキストの更新、色の変化、花火パーティクル生成
    private IEnumerator AnimateScore(int targetScore)
    {
        float duration = 1.5f; // カウントアップにかける時間
        float elapsed = 0f;
        
        Color startColor = Color.white;
        Color midColor = new Color(0f, 0.9f, 1f); // エレクトリックシアン
        Color endColor = new Color(1f, 0.8f, 0f); // 達成感を煽るゴールド

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            
            // イージング（最初は速く、後半ゆっくりに）
            float easeProgress = 1f - Mathf.Pow(1f - progress, 3f);
            int currentScore = Mathf.RoundToInt(targetScore * easeProgress);
            
            ScoreText.text = "FINAL SCORE\n" + currentScore;
            
            // スコアに応じて文字の色をダイナミックに変化させる（白 → シアン → ゴールド）
            if (progress < 0.5f)
            {
                ScoreText.color = Color.Lerp(startColor, midColor, progress * 2f);
            }
            else
            {
                ScoreText.color = Color.Lerp(midColor, endColor, (progress - 0.5f) * 2f);
            }

            yield return null;
        }

        // 最終スコアの確定
        ScoreText.text = "FINAL SCORE\n" + targetScore;
        ScoreText.color = endColor;
        
        // 確定音を再生
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(0);

        // カウントアップ終了時にド派手な花火パーティクルを生成
        SpawnCelebrationFireworks();

        // スコアを強調するため、確定後に脈動アニメーションを追加
        if (ScoreText.GetComponent<UIAnimator>() == null)
        {
            UIAnimator animator = ScoreText.gameObject.AddComponent<UIAnimator>();
            animator.PulseScale = true;
            animator.PulseAmount = 0.08f;
            animator.PulseSpeed = 2.0f;
        }
    }

    /// リザルト画面専用の豪華な花火パーティクルを生成する
    private void SpawnCelebrationFireworks()
    {
        GameObject fireworkObj = new GameObject("ResultFireworks");
        fireworkObj.transform.SetParent(ScoreText.transform, false);
        fireworkObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = fireworkObj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(200f, 500f);
        main.startSize = new ParticleSystem.MinMaxCurve(10f, 30f);
        main.gravityModifier = 0.5f;
        main.playOnAwake = false;
        main.loop = false;
        
        // ゴールドからシアン、ピンクへとランダムに変化する華やかな色設定
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0f), new Color(1f, 0.2f, 0.4f));

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 100) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 10f;

        var sizeOverTime = ps.sizeOverLifetime;
        sizeOverTime.enabled = true;
        sizeOverTime.size = new ParticleSystem.MinMaxCurve(1f, 0f);
        
        // 光の軌跡を描くためのトレイル設定
        var trails = ps.trails;
        trails.enabled = true;
        trails.ratio = 0.5f;
        trails.lifetimeMultiplier = 0.3f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // UIの上にパーティクルを描画するためのマテリアル設定
        Material glowMat = ParticleMaterialUtils.GetGlowMaterial();
        renderer.material = glowMat;
        renderer.trailMaterial = glowMat;
        // UI上に描画されるようソート順を調整
        renderer.sortingOrder = 100;

        ps.Play();
        Destroy(fireworkObj, 3f);
    }

    // Input.GetMouseButtonDownで左クリック（またはシングルタップ）を判定するための定数ID
    private const int LeftMouseButtonId = 0;

    private void Update()
    {
        // ユーザーの操作負担を減らすため、特定ボタンではなく画面全体のタップを許容
        if (Input.GetMouseButtonDown(LeftMouseButtonId))
        {
            // 決定音を再生
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySE(0);
            // タイトルシーンへ遷移
            SceneManager.LoadScene("Title");
        }
    }
}
