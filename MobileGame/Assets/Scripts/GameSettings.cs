
/// 難易度設定。スポーン間隔などのゲームバランス計算に利用される
public enum GameDifficulty { Easy, Normal, Hard }

/// Unityのシーン遷移によるデータ破棄を回避するため、設定データを静的クラスで保持する
public static class GameSettings
{
    // タイトル画面で選択され、ゲームシーンで利用される難易度の状態保持
    public static GameDifficulty Difficulty = GameDifficulty.Normal;
    // AudioManagerなどで参照されるBGMの再生許可フラグ
    public static bool BgmEnabled = true;
    // AudioManagerなどで参照されるSE（効果音）の再生許可フラグ
    public static bool SeEnabled = true;
    // PC向けエイム操作時のマウス感度倍率
    public static float MouseSensitivity = 5.0f;
}
