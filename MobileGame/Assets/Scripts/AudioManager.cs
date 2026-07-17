using UnityEngine;
using UnityEngine.SceneManagement;

/// 外部オーディオアセットへの依存をなくすため、プロシージャル生成によるサウンド再生を単一管理する
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // シーン遷移時に音が途切れるのを防ぐためのグローバルアクセスポイント
    public static AudioManager Instance;

    // BGMの種類を定義する列挙型（タイトル、ゲーム本編、リザルト）
    public enum BgmType
    {
        Title,  // タイトル画面のBGM
        Game,   // ゲーム本編のBGM
        Result  // リザルト画面のBGM
    }
    // 現在再生中のBGMの種類を保持する変数
    private BgmType currentBgm = BgmType.Title;

    // --- 波形の位相（進行度） ---
    // 主なBGM波形の現在の位相（進行状態）
    private float phaseBgm = 0f;
    // 和音などのためのサブBGM波形の位相
    private float phaseBgm2 = 0f;
    // 効果音（SE）波形の現在の位相
    private float phaseSe = 0f;

    // --- 効果音（SE）の状態管理 ---
    // 現在再生されている効果音のID（-1は何も再生されていない状態を示す）
    private int currentSoundEffectType = -1;
    // 効果音が再生され始めてからの経過時間
    private float soundEffectTime = 0f;
    // 効果音が再生されるべき合計時間（この時間を超えると再生停止）
    private float soundEffectDuration = 0f;

    // --- その他計算用 ---
    // BGMの進行に基づくビートやテンポを計算するための時間軸
    private float backgroundMusicTime = 0f;
    // デバイスのオーディオサンプリング周波数（デフォルトは44100Hz）
    private float audioSampleRate = 44100f;
    // メモリアロケーションを防ぐための自己完結型乱数シード（ノイズ生成に使用）
    private uint noiseGenerationSeed = 12345;

    // --- 音量制限 ---
    // プレイヤーの耳やデバイスのスピーカー破損を防ぐためのハードリミット（最大音量）
    private const float MaxVolume = 0.5f;

    private void Awake()
    {
        // 重複生成による音声の多重再生バグを防ぐためシングルトン化
        if (Instance == null)
        {
            // 自身のインスタンスを静的変数に保持
            Instance = this;
            // シーン遷移しても破棄されないように設定
            DontDestroyOnLoad(gameObject);
            
            // PlayerPrefsからBGMとSEの有効/無効設定を読み込む（保存されていなければ1=有効）
            GameSettings.BgmEnabled = PlayerPrefs.GetInt("BgmEnabled", 1) == 1;
            GameSettings.SeEnabled = PlayerPrefs.GetInt("SeEnabled", 1) == 1;
            
            // 強制的にオーディオを有効化（万が一オフになっていた場合の対策）
            GameSettings.BgmEnabled = true;
            GameSettings.SeEnabled = true;

            // オーディオスレッドからUnity APIにアクセスできない制約を回避するため事前にメインスレッドでキャッシュする
            audioSampleRate = (float)AudioSettings.outputSampleRate;
            // もしサンプルレートが0なら、デフォルトの44100Hzを設定
            if (audioSampleRate == 0f) audioSampleRate = 44100f;
            
            // シーン内にAudioListenerが存在しない場合、無音になるのを防ぐため自身に追加する
            if (FindObjectOfType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
                // Debug.Log("[System] AudioListenerが不足していたため、AudioManagerに追加しました。");
            }

            // OnAudioFilterRead駆動のトリガーとしてダミー再生状態を維持する
            AudioSource audioSource = GetComponent<AudioSource>();
            // 一部の環境でClipがないとAudioSourceが停止してしまう問題への対策としてダミークリップを生成
            audioSource.clip = AudioClip.Create("DummyClip", 44100, 1, 44100, false);
            audioSource.playOnAwake = true; // 起動時に自動再生
            audioSource.loop = true;        // ループ再生を有効化
            // 2Dサウンドとして再生し、カメラとの距離による減衰を防ぐ
            audioSource.spatialBlend = 0f;
            audioSource.Play();             // 再生を開始

            // シーンがロードされた時のイベントにコールバックを登録
            SceneManager.sceneLoaded += OnSceneLoaded;
            // 現在のシーンに合わせてBGMを更新
            UpdateBgmForScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            // 既にインスタンスが存在する場合は、重複したオブジェクトを破棄
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // 自身が唯一のインスタンスである場合のみ、イベントの登録を解除
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // シーンがロードされた際に呼び出されるコールバック関数
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ロードされたシーン名に基づいてBGMを切り替える
        UpdateBgmForScene(scene.name);
    }

    // シーン名に応じて再生するBGMの種類を決定し、波形をリセットする関数
    private void UpdateBgmForScene(string sceneName)
    {
        // シーン名が "Game" ならゲーム用BGM、"Result" ならリザルト用BGM、それ以外はタイトル用BGMに設定
        if (sceneName == "Game") currentBgm = BgmType.Game;
        else if (sceneName == "Result") currentBgm = BgmType.Result;
        else currentBgm = BgmType.Title;
        
        // 波形の不連続性によるポップノイズを防ぐため位相とタイマーをリセット
        backgroundMusicTime = 0f; // BGM用タイマーの初期化
        phaseBgm = 0f;            // 主波形の位相初期化
        phaseBgm2 = 0f;           // サブ波形の位相初期化
    }

    /// 入力: type(SEのID) | 出力: なし | 副作用: 再生フラグの更新
    public void PlaySE(int type)
    {
        // SEが無効に設定されている場合は即座に処理を終了
        if (!GameSettings.SeEnabled) return;
        
        // 要求されたSEの種類を保存
        currentSoundEffectType = type;
        // SE再生時間をリセット
        soundEffectTime = 0f;
        // 種類が3の場合は1.5秒、4（警告音）は0.1秒、それ以外は0.2秒の長さに設定
        if (type == 3) soundEffectDuration = 1.5f;
        else if (type == 4) soundEffectDuration = 0.1f;
        else soundEffectDuration = 0.2f;
    }

    /// 入力: isEnabled(有効か) | 出力: なし | 副作用: PlayerPrefsへの永続化、メモリ設定更新
    public void SetBGMEnabled(bool isEnabled)
    {
        // メモリ上のBGM有効フラグを更新
        GameSettings.BgmEnabled = isEnabled;
        // PlayerPrefsにBGM設定を保存（有効なら1、無効なら0）
        PlayerPrefs.SetInt("BgmEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save(); // 確実な保存のためにSave()を追加
    }

    
    /// 入力: isEnabled(有効か) | 出力: なし | 副作用: PlayerPrefsへの永続化、メモリ設定更新
    public void SetSEEnabled(bool isEnabled)
    {
        // メモリ上のSE有効フラグを更新
        GameSettings.SeEnabled = isEnabled;
        // PlayerPrefsにSE設定を保存（有効なら1、無効なら0）
        PlayerPrefs.SetInt("SeEnabled", isEnabled ? 1 : 0);
        PlayerPrefs.Save(); // 確実な保存のためにSave()を追加
    }

    /// 入力: audioData, channels | 出力: なし | 副作用: audioDataへの波形書き込み
    /// GCスパイクによる音飛びを防ぐため、本メソッド内での参照型の確保(new)は厳禁
    private void OnAudioFilterRead(float[] audioData, int channels)
    {
        // バッファ長に沿って、チャンネルごとに波形データを生成・書き込み
        for (int i = 0; i < audioData.Length; i += channels)
        {
            // BGMとSEをミックスするためのサンプリング変数
            float mixedSample = 0f;

            // BGMが有効な場合のみ波形生成を行う
            if (GameSettings.BgmEnabled)
            {
                // 残り時間が10秒を切ったらテンポとピッチを強烈に上げる（最大2.5倍）
                float tempoMultiplier = 1f;
                if (GameManager.Instance != null && !GameManager.Instance.IsGameOver && GameManager.Instance.TimeLeft <= 10f)
                {
                    tempoMultiplier = 1f + (10f - GameManager.Instance.TimeLeft) * 0.15f;
                }

                // サンプルレートに基づき経過時間を加算（テンポ倍率を掛ける）
                backgroundMusicTime += (1f / audioSampleRate) * tempoMultiplier;
                float wave = 0f;   // 生成波形
                float volume = 0f; // ボリューム係数

                if (currentBgm == BgmType.Game)
                {
                    // 焦らせるテンポの速いビート（2.5の倍速でループ）
                    float beat = (backgroundMusicTime * 2.5f) % 1f;
                    // ビートの最初の10%で周波数を高くし、打撃感を追加
                    float frequencyBgm = 65f + (beat < 0.1f ? 30f : 0f);
                    // サンプルごとの位相の増加量を計算（ピッチも一緒に上げる）
                    float incrementBgm = frequencyBgm * tempoMultiplier * 2f * Mathf.PI / audioSampleRate;
                    phaseBgm += incrementBgm;
                    // サイン波と矩形波を組み合わせてシンセサイザー風の音を合成
                    wave = Mathf.Sin(phaseBgm) * 0.5f + Mathf.Sign(Mathf.Sin(phaseBgm)) * 0.5f;
                    // ビートの進行に合わせてボリュームを減衰させる
                    volume = Mathf.Clamp01(1f - beat) * 0.15f;
                }
                else if (currentBgm == BgmType.Title)
                {
                    // 明るいアルペジオ風（4の倍速でループ）
                    float beat = (backgroundMusicTime * 4f) % 1f;
                    // 時間経過に合わせて鳴らす音階のインデックスを計算 (0, 1, 2, 3)
                    int noteIndex = (int)(backgroundMusicTime * 4f) % 4;
                    // ド、ミ、ソ、ド の周波数配列
                    float[] freqs = { 261.63f, 329.63f, 392.00f, 523.25f }; // C, E, G, C
                    float frequencyBgm = freqs[noteIndex];
                    float incrementBgm = frequencyBgm * tempoMultiplier * 2f * Mathf.PI / audioSampleRate;
                    phaseBgm += incrementBgm;
                    // 基本となるサイン波を生成
                    wave = Mathf.Sin(phaseBgm);
                    // ビートの進行に合わせてボリュームを減衰させる
                    volume = Mathf.Clamp01(1f - beat) * 0.1f;
                }
                else if (currentBgm == BgmType.Result)
                {
                    // 落ち着いた和音ベース
                    float beat = (backgroundMusicTime * 1f) % 1f;
                    // ドとミの和音を合成するための2つの周波数
                    float freq1 = 261.63f; // C
                    float freq2 = 329.63f; // E
                    float inc1 = freq1 * tempoMultiplier * 2f * Mathf.PI / audioSampleRate;
                    float inc2 = freq2 * tempoMultiplier * 2f * Mathf.PI / audioSampleRate;
                    phaseBgm += inc1;
                    phaseBgm2 += inc2;
                    // 2つのサイン波を足し合わせて半分の振幅にする
                    wave = (Mathf.Sin(phaseBgm) + Mathf.Sin(phaseBgm2)) * 0.5f;
                    // ビートが緩やかに減衰するよう調整
                    volume = Mathf.Clamp01(1f - (beat * 0.5f)) * 0.1f;
                }

                // BGMの音をミックスバッファに加算
                mixedSample += wave * volume;
            }

            // 再生中の効果音がある場合
            if (currentSoundEffectType != -1)
            {
                // 効果音の経過時間を加算
                soundEffectTime += 1f / audioSampleRate;
                // 効果音の再生時間を過ぎた場合は再生終了
                if (soundEffectTime >= soundEffectDuration)
                {
                    currentSoundEffectType = -1; // 効果音のIDをクリア
                }
                else
                {
                    float frequencySe = 440f; // デフォルトの周波数
                    float volumeSe = 0.25f;   // デフォルトのボリューム
                    float wave = 0f;          // 生成される波形データ

                    // タイプ0: 標準的のヒット音（ピッチが上昇する音）
                    if (currentSoundEffectType == 0)
                    {
                        frequencySe = 880f + (soundEffectTime * 500f);
                        float incrementSe = frequencySe * 2f * Mathf.PI / audioSampleRate;
                        phaseSe += incrementSe;
                        wave = Mathf.Sin(phaseSe); // サイン波
                    }
                    // タイプ1: ボーナス的のヒット音（2段階の高いピッチ）
                    else if (currentSoundEffectType == 1)
                    {
                        frequencySe = (soundEffectTime < 0.1f) ? 1200f : 1600f;
                        float incrementSe = frequencySe * 2f * Mathf.PI / audioSampleRate;
                        phaseSe += incrementSe;
                        wave = Mathf.Sin(phaseSe); // サイン波
                        volumeSe = 0.3f;
                    }
                    // タイプ2: リセットなどのシステム音（低音とノイズの混合）
                    else if (currentSoundEffectType == 2)
                    {
                        frequencySe = 80f;
                        float incrementSe = frequencySe * 2f * Mathf.PI / audioSampleRate;
                        phaseSe += incrementSe;
                        
                        // メモリアロケーションを避けるため線形合同法で乱数を自己生成
                        noiseGenerationSeed = (noiseGenerationSeed * 1664525 + 1013904223);
                        float noise = ((float)(noiseGenerationSeed & 0x7FFFFFFF) / 0x7FFFFFFF) * 2f - 1f;
                        
                        // 矩形波とノイズを混ぜ合わせて鈍い音を作る
                        wave = Mathf.Sign(Mathf.Sin(phaseSe)) * 0.6f + noise * 0.4f;
                        volumeSe = 0.4f;
                    }
                    // タイプ3: ブザー音（ピッチが下降するビープ音）
                    else if (currentSoundEffectType == 3)
                    {
                        frequencySe = 800f - (soundEffectTime * 600f);
                        if (frequencySe < 50f) frequencySe = 50f;
                        float incrementSe = frequencySe * 2f * Mathf.PI / audioSampleRate;
                        phaseSe += incrementSe;
                        wave = Mathf.Sign(Mathf.Sin(phaseSe)); // 矩形波
                        volumeSe = 0.3f;
                    }
                    // タイプ4: タイマー警告音（高く短いビープ音）
                    else if (currentSoundEffectType == 4)
                    {
                        frequencySe = 1000f;
                        float incrementSe = frequencySe * 2f * Mathf.PI / audioSampleRate;
                        phaseSe += incrementSe;
                        wave = Mathf.Sin(phaseSe); // サイン波
                        volumeSe = 0.2f;
                    }

                    // SEの音をミックスバッファに加算
                    mixedSample += wave * volumeSe;
                }
            }

            // 異常な振幅によるスピーカー破損を防ぐため出力をハードリミット
            mixedSample = Mathf.Clamp(mixedSample, -MaxVolume, MaxVolume);

            // ステレオ等のチャンネル数分、同じサンプルデータをバッファに書き込む
            for (int channelIndex = 0; channelIndex < channels; channelIndex++)
            {
                audioData[i + channelIndex] = mixedSample;
            }
        }
    }
}
