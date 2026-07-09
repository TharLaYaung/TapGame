using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// シーン間の遷移における黒フェードイン/フェードアウト演出を管理する
public class SceneFader : MonoBehaviour
{
    // シーン遷移時にフェード効果を呼び出すためのグローバルアクセスポイント
    public static SceneFader Instance { get; private set; }

    // UIや他要素の手前に黒画面を覆いかぶせるための最前面（マイナス値）深度設定
    private const int DefaultDrawDepth = -1000;
    // フェードの暗転/明転にかかる基本速度
    private const float DefaultFadeSpeed = 2.0f;
    // 不透明度を下げる（明転する）ための方向係数
    private const int FadeInDirection = -1;
    // 不透明度を上げる（暗転する）ための方向係数
    private const int FadeOutDirection = 1;

    // フェード描画に用いる真っ黒な1x1テクスチャ
    private Texture2D blackTexture;
    // 現在の黒テクスチャの不透明度（1で完全暗転、0で完全透明）
    private float alpha = 1.0f;
    // GUI描画時の重なり順（マイナス値ほど手前に描画される）
    private int DrawDepth = DefaultDrawDepth;
    // フェードの進行速度（1秒あたりのアルファ値の変化量）
    private float FadeSpeed = DefaultFadeSpeed;
    // 現在のフェード進行方向（暗転=1か明転=-1か）
    private int fadeDirection = FadeInDirection;

    private void Awake()
    {
        // 複数生成によるステート破壊を防ぐためシングルトン化
        if (Instance == null)
        {
            // インスタンスを登録
            Instance = this;
            // シーン遷移してもフェーダーが破棄されないよう設定
            DontDestroyOnLoad(gameObject);
            
            // パフォーマンス低下を防ぐため、1x1の単色テクスチャをメモリ上に事前生成
            blackTexture = new Texture2D(1, 1);
            // テクスチャの唯一のピクセルを黒色に設定
            blackTexture.SetPixel(0, 0, Color.black);
            // 変更をテクスチャに適用
            blackTexture.Apply();
            
            // 新しいシーンのロードが完了した時のコールバックを登録
            SceneManager.sceneLoaded += OnLevelFinishedLoading;
        }
        else
        {
            // 既にインスタンスが存在する場合は重複オブジェクトを破棄
            Destroy(gameObject);
        }
    }

    private void OnGUI()
    {
        // 方向と速度、経過時間を元にアルファ値を増減させる
        alpha += fadeDirection * FadeSpeed * Time.deltaTime;
        // アルファ値が0未満や1を超えないよう0.0〜1.0の範囲に制限する
        alpha = Mathf.Clamp01(alpha);
        
        // GUIの描画色（主にアルファ値）を現在の状態に更新する
        GUI.color = new Color(GUI.color.r, GUI.color.g, GUI.color.b, alpha);
        
        // 他のUI要素の上に強制描画するため深度値をマイナスに設定
        GUI.depth = DrawDepth;
        // 画面全体を覆うように黒色テクスチャを描画する
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTexture);
    }

    // シーンのロードが完了した際に自動的に呼ばれるメソッド
    private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        // 画面を完全に暗転状態（アルファ値1）にセット
        alpha = 1f;
        // 新しいシーンが始まったので、画面を明るくしていく（フェードイン）
        FadeIn();
    }

    // 画面を明るくする（暗転を解除する）処理を開始
    public void FadeIn()
    {
        // 進行方向を「明転（マイナス方向）」に設定
        fadeDirection = FadeInDirection;
    }

    // 画面を暗くする処理を開始
    public void FadeOut()
    {
        // 進行方向を「暗転（プラス方向）」に設定
        fadeDirection = FadeOutDirection;
    }

    /// 指定シーンへのフェードアウト付き遷移リクエスト
    /// 入力: sceneName (ロード対象のシーン名)
    /// 副作用: フェードアウトアニメーションを実行後、UnityのシーンロードAPIを呼び出す
    public void FadeAndLoadScene(string sceneName)
    {
        // フェードアウトとシーンロードを非同期で実行するためのコルーチンを開始
        StartCoroutine(FadeAndLoadRoutine(sceneName));
    }

    // フェードアウトの完了を待ってからシーン遷移を行うコルーチン処理
    private IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        // まず画面を暗転させる
        FadeOut();
        // フェードアウトの完了（アルファが1になるまでの時間）を待機してからロード処理に移行
        yield return new WaitForSeconds(1f / FadeSpeed);
        // 指定されたシーンをロード
        SceneManager.LoadScene(sceneName);
    }
}
